const express = require('express');
const db = require('../db');
const { requireAuth, formatMsg } = require('../helpers');
const { broadcastMessage, broadcastGroupMessage, broadcastEdit, broadcastDelete, broadcastGroupEvent } = require('../ws');

const router = express.Router();

router.get('/chats/:chatId/messages', requireAuth, (req, res) => {
  const page = Math.max(1, parseInt(req.query.page) || 1);
  const limit = 50;
  const offset = (page - 1) * limit;
  const messages = db.prepare('SELECT * FROM messages WHERE chat_id = ? ORDER BY created_at DESC LIMIT ? OFFSET ?').all(req.params.chatId, limit, offset);
  db.prepare('UPDATE messages SET is_read = 1 WHERE chat_id = ? AND sender_id != ? AND is_read = 0').run(req.params.chatId, req.userId);
  res.json(messages.reverse().map(formatMsg));
});

router.get('/groups/:groupId/messages', requireAuth, (req, res) => {
  const page = Math.max(1, parseInt(req.query.page) || 1);
  const limit = 50;
  const offset = (page - 1) * limit;
  const messages = db.prepare('SELECT * FROM messages WHERE group_id = ? ORDER BY created_at DESC LIMIT ? OFFSET ?').all(req.params.groupId, limit, offset);
  db.prepare('UPDATE messages SET is_read = 1 WHERE group_id = ? AND sender_id != ? AND is_read = 0').run(req.params.groupId, req.userId);
  res.json(messages.reverse().map(formatMsg));
});

router.post('/chats/:chatId/messages', requireAuth, (req, res) => {
  const { content, mediaUrl, mediaType, replyToId } = req.body;
  if (!content && !mediaUrl) return res.status(400).json({ message: 'Content or media required' });
  const chat = db.prepare('SELECT id FROM chats WHERE id = ? AND (user1_id = ? OR user2_id = ?)').get(req.params.chatId, req.userId, req.userId);
  if (!chat) return res.status(403).json({ message: 'Not a participant' });
  const result = db.prepare('INSERT INTO messages (chat_id, sender_id, content, media_url, media_type, reply_to_id) VALUES (?, ?, ?, ?, ?, ?)')
    .run(req.params.chatId, req.userId, content || '', mediaUrl || null, mediaType || null, replyToId || null);
  const message = { id: result.lastInsertRowid, chatId: parseInt(req.params.chatId), senderId: req.userId, content: content || '',
    mediaUrl: mediaUrl || null, mediaType: mediaType || null, replyToId: replyToId || null, sentAt: new Date().toISOString(), isRead: false, isEdited: false, isDeleted: false };
  broadcastMessage(req.params.chatId, message);
  res.json(message);
});

router.post('/groups/:groupId/messages', requireAuth, (req, res) => {
  const { content, mediaUrl, mediaType, replyToId } = req.body;
  if (!content && !mediaUrl) return res.status(400).json({ message: 'Content or media required' });
  const isMember = db.prepare('SELECT 1 FROM group_members WHERE group_id = ? AND user_id = ?').get(req.params.groupId, req.userId);
  if (!isMember) return res.status(403).json({ message: 'Not a member' });
  const result = db.prepare('INSERT INTO messages (group_id, sender_id, content, media_url, media_type, reply_to_id) VALUES (?, ?, ?, ?, ?, ?)')
    .run(req.params.groupId, req.userId, content || '', mediaUrl || null, mediaType || null, replyToId || null);
  const message = { id: result.lastInsertRowid, groupId: parseInt(req.params.groupId), senderId: req.userId, content: content || '',
    mediaUrl: mediaUrl || null, mediaType: mediaType || null, replyToId: replyToId || null, sentAt: new Date().toISOString(), isRead: false, isEdited: false, isDeleted: false };
  broadcastGroupMessage(req.params.groupId, message);
  res.json(message);
});

router.put('/chats/:chatId/messages/:messageId', requireAuth, (req, res) => {
  const { content } = req.body;
  if (!content || typeof content !== 'string') return res.status(400).json({ message: 'Content required' });
  const msg = db.prepare('SELECT * FROM messages WHERE id = ?').get(req.params.messageId);
  if (!msg) return res.status(404).json({ message: 'Not found' });
  if (msg.sender_id !== req.userId) return res.status(403).json({ message: 'Not your message' });
  db.prepare('UPDATE messages SET content = ?, is_edited = 1 WHERE id = ?').run(content, req.params.messageId);
  const targetId = msg.chat_id || msg.group_id;
  const updated = { id: parseInt(req.params.messageId), content, isEdited: true };
  if (msg.chat_id) broadcastEdit(targetId, updated);
  else broadcastGroupEvent(targetId, { type: 'edit', message: updated });
  res.json(updated);
});

router.delete('/chats/:chatId/messages/:messageId', requireAuth, (req, res) => {
  const msg = db.prepare('SELECT * FROM messages WHERE id = ?').get(req.params.messageId);
  if (!msg) return res.status(404).json({ message: 'Not found' });
  if (msg.sender_id !== req.userId) return res.status(403).json({ message: 'Not your message' });
  db.prepare('UPDATE messages SET is_deleted = 1, content = ? WHERE id = ?').run('', req.params.messageId);
  const targetId = msg.chat_id || msg.group_id;
  if (msg.chat_id) broadcastDelete(targetId, parseInt(req.params.messageId));
  else broadcastGroupEvent(targetId, { type: 'delete', messageId: parseInt(req.params.messageId) });
  res.json({ ok: true });
});

router.get('/chats/:chatId/search', requireAuth, (req, res) => {
  const q = (req.query.q || '').trim();
  if (q.length < 1) return res.json([]);
  const like = `%${q.replace(/[%_]/g, '\\$&')}%`;
  const messages = db.prepare(`SELECT * FROM messages WHERE chat_id = ? AND is_deleted = 0 AND content LIKE ? ESCAPE '\\' ORDER BY created_at DESC LIMIT 20`)
    .all(req.params.chatId, like);
  res.json(messages.reverse().map(formatMsg));
});

router.get('/groups/:groupId/search', requireAuth, (req, res) => {
  const q = (req.query.q || '').trim();
  if (q.length < 1) return res.json([]);
  const like = `%${q.replace(/[%_]/g, '\\$&')}%`;
  const messages = db.prepare(`SELECT * FROM messages WHERE group_id = ? AND is_deleted = 0 AND content LIKE ? ESCAPE '\\' ORDER BY created_at DESC LIMIT 20`)
    .all(req.params.groupId, like);
  res.json(messages.reverse().map(formatMsg));
});

module.exports = router;
