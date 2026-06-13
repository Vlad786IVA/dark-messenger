const express = require('express');
const db = require('../db');
const { requireAuth } = require('../helpers');
const { avatarUpload } = require('./upload');

const router = express.Router();

router.get('/search', requireAuth, (req, res) => {
  const q = (req.query.q || '').trim();
  if (q.length < 1) return res.json([]);
  const like = `%${q.replace(/[%_]/g, '\\$&')}%`;
  const users = db.prepare(`SELECT id, username, display_name, is_online, last_seen, avatar_url FROM users WHERE id != ? AND (username LIKE ? OR display_name LIKE ?) LIMIT 20`)
    .all(req.userId, like, like);
  res.json(users.map(u => ({ userId: u.id, username: u.username, displayName: u.display_name, isOnline: !!u.is_online, lastSeen: u.last_seen, avatarUrl: u.avatar_url })));
});

router.put('/profile', requireAuth, (req, res) => {
  const { displayName } = req.body;
  if (!displayName || typeof displayName !== 'string') return res.status(400).json({ message: 'Display name required' });
  db.prepare('UPDATE users SET display_name = ? WHERE id = ?').run(displayName.trim(), req.userId);
  res.json({ ok: true, displayName: displayName.trim() });
});

router.post('/avatar', requireAuth, avatarUpload.single('avatar'), (req, res) => {
  if (!req.file) return res.status(400).json({ message: 'No file' });
  const avatarUrl = `/uploads/${req.file.filename}`;
  db.prepare('UPDATE users SET avatar_url = ? WHERE id = ?').run(avatarUrl, req.userId);
  res.json({ avatarUrl });
});

module.exports = router;
