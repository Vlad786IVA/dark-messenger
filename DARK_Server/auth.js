const jwt = require('jsonwebtoken');

const SECRET = process.env.JWT_SECRET || 'dark_messenger_secret_key_2024';
const EXPIRES_IN = '7d';

function generateToken(userId, username) {
  return jwt.sign({ userId, username }, SECRET, { expiresIn: EXPIRES_IN });
}

function verifyToken(token) {
  try {
    return jwt.verify(token, SECRET);
  } catch {
    return null;
  }
}

module.exports = { generateToken, verifyToken };
