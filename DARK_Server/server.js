const express = require('express');
const cors = require('cors');
const http = require('http');
const path = require('path');
const db = require('./db');
const { createWebSocketServer } = require('./ws');

const app = express();
const server = http.createServer(app);

app.use(cors({ origin: true, credentials: true }));
app.use(express.json({ limit: '50mb' }));
app.use('/uploads', express.static(path.join(__dirname, 'uploads')));

app.get('/api/health', (req, res) => {
  try {
    db.prepare('SELECT 1').get();
    res.json({ status: 'ok', db: 'connected' });
  } catch {
    res.status(503).json({ status: 'error', db: 'disconnected' });
  }
});

app.use('/api/auth', require('./routes/auth'));
app.use('/api/chats', require('./routes/chats'));
app.use('/api/groups', require('./routes/groups'));
app.use('/api', require('./routes/messages'));
app.use('/api/upload', require('./routes/upload'));
app.use('/api/users', require('./routes/users'));

createWebSocketServer(server);

const PORT = process.env.PORT || 8080;
server.listen(PORT, '0.0.0.0', () => {
  console.log(`DARK Server running on port ${PORT}`);
  console.log(`  API:      http://0.0.0.0:${PORT}/api`);
  console.log(`  Uploads:  http://0.0.0.0:${PORT}/uploads`);
  console.log(`  Health:   http://0.0.0.0:${PORT}/api/health`);
});
