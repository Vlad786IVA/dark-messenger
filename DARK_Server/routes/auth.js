const express = require('express');
const bcrypt = require('bcryptjs');
const rateLimit = require('express-rate-limit');
const db = require('../db');
const auth = require('../auth');

const router = express.Router();
const authLimiter = rateLimit({ windowMs: 60_000, max: 10, message: { message: 'Too many attempts' } });

function createSession(userId, token, req) {
  const device = (req.headers['user-agent'] || 'Unknown').substring(0, 100);
  const ip = req.ip || req.connection?.remoteAddress || '';
  db.prepare('INSERT INTO sessions (user_id, token, device_name, ip) VALUES (?, ?, ?, ?)').run(userId, token, device, ip);
}

router.post('/register', authLimiter, (req, res) => {
  const { username, displayName, password } = req.body;
  if (!username || !password) return res.status(400).json({ message: 'Username and password required' });
  if (username.length < 2 || username.length > 32) return res.status(400).json({ message: 'Username 2-32 chars' });
  if (password.length < 1) return res.status(400).json({ message: 'Password required' });
  const existing = db.prepare('SELECT id FROM users WHERE username = ?').get(username);
  if (existing) return res.status(409).json({ message: 'Username already taken' });
  const hash = bcrypt.hashSync(password, 10);
  const result = db.prepare('INSERT INTO users (username, display_name, password_hash) VALUES (?, ?, ?)').run(username, displayName || username, hash);
  const token = auth.generateToken(result.lastInsertRowid, username);
  createSession(result.lastInsertRowid, token, req);
  res.json({ token, userId: result.lastInsertRowid });
});

router.post('/login', authLimiter, (req, res) => {
  const { username, password } = req.body;
  if (!username || !password) return res.status(400).json({ message: 'Username and password required' });
  const user = db.prepare('SELECT * FROM users WHERE username = ?').get(username);
  if (!user || !bcrypt.compareSync(password, user.password_hash)) return res.status(401).json({ message: 'Invalid credentials' });
  db.prepare('UPDATE users SET is_online = 1 WHERE id = ?').run(user.id);
  const token = auth.generateToken(user.id, user.username);
  createSession(user.id, token, req);
  res.json({ token, userId: user.id, displayName: user.display_name, username: user.username });
});

router.post('/reset-password', authLimiter, (req, res) => {
  const { username, password } = req.body;
  if (!username || !password) return res.status(400).json({ message: 'Username and password required' });
  const user = db.prepare('SELECT id FROM users WHERE username = ?').get(username);
  if (!user) return res.status(404).json({ message: 'User not found' });
  const hash = bcrypt.hashSync(password, 10);
  db.prepare('UPDATE users SET password_hash = ? WHERE id = ?').run(hash, user.id);
  res.json({ message: 'Password updated' });
});

router.get('/sessions', (req, res) => {
  const payload = auth.verifyToken(req.headers.authorization?.split(' ')[1]);
  if (!payload) return res.status(401).json({ message: 'Unauthorized' });
  const sessions = db.prepare('SELECT id, device_name, ip, created_at, last_active FROM sessions WHERE user_id = ? ORDER BY last_active DESC').all(payload.userId);
  res.json(sessions);
});

router.delete('/sessions/:id', (req, res) => {
  const payload = auth.verifyToken(req.headers.authorization?.split(' ')[1]);
  if (!payload) return res.status(401).json({ message: 'Unauthorized' });
  const session = db.prepare('SELECT * FROM sessions WHERE id = ? AND user_id = ?').get(req.params.id, payload.userId);
  if (!session) return res.status(404).json({ message: 'Session not found' });
  db.prepare('DELETE FROM sessions WHERE id = ?').run(req.params.id);
  res.json({ message: 'Session terminated' });
});

module.exports = router;
