const db = require('./db');
const auth = require('./auth');

function requireAuth(req, res, next) {
  const header = req.headers.authorization;
  if (!header || !header.startsWith('Bearer ')) return res.status(401).json({ message: 'No token' });
  const payload = auth.verifyToken(header.slice(7));
  if (!payload) return res.status(401).json({ message: 'Invalid token' });
  req.userId = payload.userId;
  req.username = payload.username;
  next();
}

function formatMsg(m) {
  return { id: m.id, chatId: m.chat_id, groupId: m.group_id, senderId: m.sender_id, content: m.content,
    mediaUrl: m.media_url, mediaType: m.media_type, replyToId: m.reply_to_id, sentAt: m.created_at,
    isRead: !!m.is_read, isEdited: !!m.is_edited, isDeleted: !!m.is_deleted };
}

function getChatUserIds(chatId) {
  const chat = db.prepare('SELECT user1_id, user2_id FROM chats WHERE id = ?').get(chatId);
  return chat ? [chat.user1_id, chat.user2_id] : [];
}

function getGroupUserIds(groupId) {
  const members = db.prepare('SELECT user_id FROM group_members WHERE group_id = ?').all(groupId);
  return members.map(m => m.user_id);
}

function getContactIds(userId) {
  const rows = db.prepare('SELECT user1_id, user2_id FROM chats WHERE user1_id = ? OR user2_id = ?').all(userId, userId);
  const ids = new Set();
  rows.forEach(r => { ids.add(r.user1_id); ids.add(r.user2_id); });
  ids.delete(userId);
  return [...ids];
}

module.exports = { requireAuth, formatMsg, getChatUserIds, getGroupUserIds, getContactIds };
