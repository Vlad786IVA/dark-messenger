const WebSocket = require('ws');
const auth = require('./auth');
const db = require('./db');
const { getChatUserIds, getGroupUserIds, getContactIds } = require('./helpers');

const clients = new Map();

function createWebSocketServer(server) {
  const wss = new WebSocket.Server({ server });

  wss.on('connection', (ws) => {
    let userId = null;

    ws.on('message', (data) => {
      try {
        const msg = JSON.parse(data);
        if (msg.type === 'auth') {
          const payload = auth.verifyToken(msg.token);
          if (payload) {
            userId = payload.userId;
            clients.set(userId, ws);
            db.prepare('UPDATE users SET is_online = 1 WHERE id = ?').run(userId);
            db.prepare("UPDATE sessions SET last_active = CURRENT_TIMESTAMP WHERE user_id = ? AND token = ?").run(userId, msg.token);
            broadcastOnlineStatus(userId, true);
          }
        } else if (msg.type === 'typing') {
          if (!userId) return;
          if (msg.chatId) broadcastTyping(msg.chatId, userId, true);
          if (msg.groupId) broadcastGroupTyping(msg.groupId, userId, true);
        } else if (msg.type === 'stop_typing') {
          if (!userId) return;
          if (msg.chatId) broadcastTyping(msg.chatId, userId, false);
          if (msg.groupId) broadcastGroupTyping(msg.groupId, userId, false);
        }
      } catch (e) {
        console.error('WS message error:', e.message);
      }
    });

    ws.on('close', () => {
      if (userId) {
        clients.delete(userId);
        db.prepare('UPDATE users SET is_online = 0, last_seen = CURRENT_TIMESTAMP WHERE id = ?').run(userId);
        broadcastOnlineStatus(userId, false);
      }
    });

    ws.on('error', (e) => console.error('WS error:', e.message));
  });

  setInterval(() => {
    const stale = [];
    for (const [uid, ws] of clients) if (ws.readyState !== WebSocket.OPEN) stale.push(uid);
    stale.forEach(uid => clients.delete(uid));
  }, 60_000);
}

function sendToUser(uid, payload) {
  const client = clients.get(uid);
  if (client && client.readyState === WebSocket.OPEN) {
    client.send(payload);
  } else if (client) {
    clients.delete(uid);
  }
}

function broadcastOnlineStatus(userId, online) {
  const payload = JSON.stringify({ type: 'online', userId, online });
  getContactIds(userId).forEach(uid => sendToUser(uid, payload));
}

function broadcastMessage(chatId, message) {
  const payload = JSON.stringify({ type: 'message', chatId, message });
  getChatUserIds(chatId).forEach(uid => sendToUser(uid, payload));
}

function broadcastGroupMessage(groupId, message) {
  const payload = JSON.stringify({ type: 'group_message', groupId, message });
  getGroupUserIds(groupId).forEach(uid => sendToUser(uid, payload));
}

function broadcastEdit(chatId, message) {
  const payload = JSON.stringify({ type: 'edit', chatId, message });
  getChatUserIds(chatId).forEach(uid => sendToUser(uid, payload));
}

function broadcastDelete(chatId, messageId) {
  const payload = JSON.stringify({ type: 'delete', chatId, messageId });
  getChatUserIds(chatId).forEach(uid => sendToUser(uid, payload));
}

function broadcastTyping(chatId, userId, isTyping) {
  const payload = JSON.stringify({ type: 'typing', chatId, userId, isTyping });
  getChatUserIds(chatId).forEach(uid => sendToUser(uid, payload));
}

function broadcastGroupTyping(groupId, userId, isTyping) {
  const payload = JSON.stringify({ type: 'typing', groupId, userId, isTyping });
  getGroupUserIds(groupId).forEach(uid => sendToUser(uid, payload));
}

function broadcastGroupEvent(groupId, event) {
  const payload = JSON.stringify({ ...event, groupId });
  getGroupUserIds(groupId).forEach(uid => sendToUser(uid, payload));
}

module.exports = { createWebSocketServer, broadcastMessage, broadcastGroupMessage,
  broadcastEdit, broadcastDelete, broadcastGroupEvent };
