const express = require('express');
const db = require('../db');
const { requireAuth } = require('../helpers');

const router = express.Router();

router.get('/', requireAuth, (req, res) => {
  const chats = db.prepare(`
    SELECT c.*,
      CASE WHEN c.user1_id = ? THEN u2.username ELSE u1.username END as other_username,
      CASE WHEN c.user1_id = ? THEN u2.display_name ELSE u1.display_name END as other_display_name,
      CASE WHEN c.user1_id = ? THEN u2.is_online ELSE u1.is_online END as other_online,
      CASE WHEN c.user1_id = ? THEN u2.avatar_url ELSE u1.avatar_url END as other_avatar,
      m.content as last_message, m.created_at as last_message_time,
      (SELECT COUNT(*) FROM messages WHERE chat_id = c.id AND sender_id != ? AND is_read = 0 AND is_deleted = 0) as unread_count
    FROM chats c JOIN users u1 ON c.user1_id = u1.id JOIN users u2 ON c.user2_id = u2.id
    LEFT JOIN messages m ON m.chat_id = c.id AND m.created_at = (SELECT MAX(created_at) FROM messages WHERE chat_id = c.id AND is_deleted = 0)
    WHERE c.user1_id = ? OR c.user2_id = ?
    ORDER BY COALESCE(m.created_at, c.created_at) DESC
  `).all(req.userId, req.userId, req.userId, req.userId, req.userId, req.userId, req.userId);

  res.json(chats.map(c => ({ id: c.id, name: c.other_display_name || c.other_username, avatarUrl: c.other_avatar,
    lastMessage: c.last_message || '', lastMessageTime: c.last_message_time || c.created_at, unreadCount: c.unread_count,
    isGroup: false, otherUser: { id: c.user1_id === req.userId ? c.user2_id : c.user1_id, username: c.other_username,
      displayName: c.other_display_name, isOnline: c.other_online, avatarUrl: c.other_avatar } })));
});

router.post('/', requireAuth, (req, res) => {
  const { userId: targetUserId } = req.body;
  if (!targetUserId) return res.status(400).json({ message: 'userId required' });
  if (targetUserId === req.userId) return res.status(400).json({ message: 'Cannot chat with yourself' });
  let chat = db.prepare('SELECT * FROM chats WHERE (user1_id = ? AND user2_id = ?) OR (user1_id = ? AND user2_id = ?)')
    .get(req.userId, targetUserId, targetUserId, req.userId);
  if (!chat) {
    const result = db.prepare('INSERT INTO chats (user1_id, user2_id) VALUES (?, ?)').run(req.userId, targetUserId);
    chat = { id: result.lastInsertRowid };
  }
  res.json({ id: chat.id });
});

router.delete('/:chatId', requireAuth, (req, res) => {
  const chat = db.prepare('SELECT * FROM chats WHERE id = ? AND (user1_id = ? OR user2_id = ?)').get(req.params.chatId, req.userId, req.userId);
  if (!chat) return res.status(404).json({ message: 'Chat not found' });
  db.prepare('DELETE FROM messages WHERE chat_id = ?').run(req.params.chatId);
  db.prepare('DELETE FROM chats WHERE id = ?').run(req.params.chatId);
  res.json({ ok: true });
});

module.exports = router;
