const express = require('express');
const db = require('../db');
const { requireAuth } = require('../helpers');

const router = express.Router();

router.get('/', requireAuth, (req, res) => {
  const groups = db.prepare(`
    SELECT g.*, m.content as last_message, m.created_at as last_message_time,
      (SELECT COUNT(*) FROM messages WHERE group_id = g.id AND sender_id != ? AND is_read = 0 AND is_deleted = 0) as unread_count
    FROM group_chats g
    JOIN group_members gm ON g.id = gm.group_id AND gm.user_id = ?
    LEFT JOIN messages m ON m.group_id = g.id AND m.created_at = (SELECT MAX(created_at) FROM messages WHERE group_id = g.id AND is_deleted = 0)
    ORDER BY COALESCE(m.created_at, g.created_at) DESC
  `).all(req.userId, req.userId);

  res.json(groups.map(g => ({ id: g.id, name: g.name, avatarUrl: g.avatar_url, lastMessage: g.last_message || '',
    lastMessageTime: g.last_message_time || g.created_at, unreadCount: g.unread_count, isGroup: true, createdBy: g.created_by })));
});

router.post('/', requireAuth, (req, res) => {
  const { name, memberIds } = req.body;
  if (!name || typeof name !== 'string' || name.trim().length === 0) return res.status(400).json({ message: 'Name required' });
  if (!memberIds || !Array.isArray(memberIds) || memberIds.length === 0) return res.status(400).json({ message: 'Members required' });
  const safeName = name.trim().slice(0, 64);
  const result = db.prepare('INSERT INTO group_chats (name, created_by) VALUES (?, ?)').run(safeName, req.userId);
  const groupId = result.lastInsertRowid;
  const ins = db.prepare('INSERT OR IGNORE INTO group_members (group_id, user_id, role) VALUES (?, ?, ?)');
  ins.run(groupId, req.userId, 'creator');
  const uniqueIds = [...new Set(memberIds.filter(id => id !== req.userId))];
  uniqueIds.forEach(id => ins.run(groupId, id, 'member'));
  res.json({ id: groupId });
});

router.get('/:groupId/members', requireAuth, (req, res) => {
  const isMember = db.prepare('SELECT 1 FROM group_members WHERE group_id = ? AND user_id = ?').get(req.params.groupId, req.userId);
  if (!isMember) return res.status(403).json({ message: 'Not a member' });
  const members = db.prepare(`SELECT u.id, u.username, u.display_name, u.is_online, u.avatar_url, gm.role
    FROM group_members gm JOIN users u ON gm.user_id = u.id WHERE gm.group_id = ?`).all(req.params.groupId);
  res.json(members.map(m => ({ userId: m.id, username: m.username, displayName: m.display_name, isOnline: m.is_online, avatarUrl: m.avatar_url, role: m.role })));
});

router.delete('/:groupId', requireAuth, (req, res) => {
  const isCreator = db.prepare('SELECT 1 FROM group_members WHERE group_id = ? AND user_id = ? AND role = ?').get(req.params.groupId, req.userId, 'creator');
  if (!isCreator) return res.status(403).json({ message: 'Only creator can delete group' });
  db.prepare('DELETE FROM messages WHERE group_id = ?').run(req.params.groupId);
  db.prepare('DELETE FROM group_members WHERE group_id = ?').run(req.params.groupId);
  db.prepare('DELETE FROM group_chats WHERE id = ?').run(req.params.groupId);
  res.json({ ok: true });
});

module.exports = router;
