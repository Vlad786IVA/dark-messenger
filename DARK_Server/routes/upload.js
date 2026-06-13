const express = require('express');
const path = require('path');
const multer = require('multer');
const crypto = require('crypto');
const fs = require('fs');
const { requireAuth } = require('../helpers');

const router = express.Router();

const uploadsDir = path.join(__dirname, '..', 'uploads');
if (!fs.existsSync(uploadsDir)) fs.mkdirSync(uploadsDir, { recursive: true });

const storage = multer.diskStorage({
  destination: (req, file, cb) => cb(null, uploadsDir),
  filename: (req, file, cb) => {
    const ext = path.extname(file.originalname);
    const safe = Date.now().toString(36) + crypto.randomUUID().slice(0, 8) + ext;
    cb(null, safe);
  }
});

const upload = multer({ storage, limits: { fileSize: 100 * 1024 * 1024 } });
const avatarUpload = multer({ storage, limits: { fileSize: 5 * 1024 * 1024 } });

router.post('/', requireAuth, upload.single('file'), (req, res) => {
  if (!req.file) return res.status(400).json({ message: 'No file uploaded' });
  const fileUrl = `/uploads/${req.file.filename}`;
  const mimeType = req.file.mimetype;
  let mediaType = 'file';
  if (mimeType.startsWith('image/')) mediaType = 'image';
  else if (mimeType.startsWith('video/')) mediaType = 'video';
  else if (mimeType.startsWith('audio/')) mediaType = 'audio';
  res.json({ url: fileUrl, mediaType, fileName: req.file.originalname, size: req.file.size });
});

module.exports = router;
module.exports.avatarUpload = avatarUpload;
