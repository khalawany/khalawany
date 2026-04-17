(() => {
  const startBtn = document.getElementById('startRecord');
  const stopBtn = document.getElementById('stopRecord');
  const status = document.getElementById('recordStatus');
  const mediaTypeSelect = document.getElementById('mediaType');
  const previewVideo = document.getElementById('previewVideo');

  if (!startBtn || !stopBtn) return;

  let mediaRecorder;
  let chunks = [];
  let stream;

  function getSupportedMimeType(isVideo) {
    const candidates = isVideo
      ? ['video/webm;codecs=vp9,opus', 'video/webm;codecs=vp8,opus', 'video/webm', 'video/mp4']
      : ['audio/webm;codecs=opus', 'audio/webm', 'audio/mp4'];

    return candidates.find(type => MediaRecorder.isTypeSupported(type)) || '';
  }

  startBtn.addEventListener('click', async () => {
    try {
      const isVideo = mediaTypeSelect?.value !== 'audio';
      stream = await navigator.mediaDevices.getUserMedia({
        audio: {
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true
        },
        video: isVideo
      });

      if (isVideo && previewVideo) {
        previewVideo.style.display = 'block';
        previewVideo.srcObject = stream;
      }

      const mimeType = getSupportedMimeType(isVideo);
      mediaRecorder = mimeType
        ? new MediaRecorder(stream, { mimeType })
        : new MediaRecorder(stream);
      chunks = [];

      mediaRecorder.ondataavailable = (event) => {
        if (event.data && event.data.size > 0) chunks.push(event.data);
      };

      mediaRecorder.onstop = () => {
        const resolvedType = mimeType || (isVideo ? 'video/webm' : 'audio/webm');
        const blob = new Blob(chunks, { type: resolvedType });
        const extension = resolvedType.includes('mp4') ? 'mp4' : 'webm';
        const file = new File([blob], `recorded-${Date.now()}.${extension}`, { type: resolvedType });

        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);

        const input = document.getElementById('fileInput');
        if (input) input.files = dataTransfer.files;

        status.textContent = 'Recorded and attached to upload form.';

        stream?.getTracks().forEach(t => t.stop());
        if (previewVideo) previewVideo.srcObject = null;
      };

      mediaRecorder.start(1000);
      status.textContent = 'Recording... (microphone is enabled)';
    } catch (err) {
      status.textContent = 'Unable to start recording. Check mic/camera permissions in browser.';
    }
  });

  stopBtn.addEventListener('click', () => {
    if (mediaRecorder && mediaRecorder.state !== 'inactive') {
      mediaRecorder.stop();
    }
  });
})();
