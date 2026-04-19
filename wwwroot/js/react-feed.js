(() => {
  if (!window.React || !window.ReactDOM) return;

  const e = React.createElement;

  function MediaPlayer({ clip }) {
    if (clip.mediaType === 'video') {
      return e('video', { src: clip.filePath, controls: true, width: 420 });
    }
    return e('audio', { src: clip.filePath, controls: true });
  }

  function ClipCard({ clip, manage }) {
    return e('div', { className: 'card' },
      e('h3', null, clip.title),
      e('p', null, clip.description || ''),
      e('small', null, `By ${clip.ownerDisplayName || 'Unknown'} on ${clip.createdAtLocal || ''}`),
      e('div', null, e(MediaPlayer, { clip })),
      manage ? e('div', null,
        e('div', { className: 'muted-note' }, `Status: ${clip.isShared ? 'Shared' : 'Private'}`),
        e('a', { href: `/Media/Edit/${clip.id}` }, 'Edit'),
        e('form', { className: 'inline-form', method: 'post', action: `/Media/ToggleShare/${clip.id}` },
          e('button', { type: 'submit' }, clip.isShared ? 'Make Private' : 'Share')
        ),
        e('form', {
          className: 'inline-form', method: 'post', action: `/Media/Delete/${clip.id}`,
          onSubmit: (ev) => {
            if (!window.confirm('Delete this clip?')) ev.preventDefault();
          }
        }, e('button', { type: 'submit' }, 'Delete'))
      ) : null
    );
  }

  function HomeFeed({ clips }) {
    return e(React.Fragment, null,
      e('h1', null, 'Family Local Network Tube'),
      e('p', null, 'Only shared family clips are visible here. Your private clips are visible only to you (and admin).'),
      clips.length === 0 ? e('p', null, 'No clips available yet.') : null,
      ...clips.map(c => e(ClipCard, { key: c.id, clip: c, manage: false }))
    );
  }

  function MyClips({ clips }) {
    return e(React.Fragment, null,
      e('h2', null, 'My Clips'),
      clips.length === 0 ? e('p', null, 'No uploads yet.') : null,
      ...clips.map(c => e(ClipCard, { key: c.id, clip: c, manage: true }))
    );
  }

  function parseData(scriptId) {
    const tag = document.getElementById(scriptId);
    if (!tag) return [];
    try {
      return JSON.parse(tag.textContent || '[]');
    } catch {
      return [];
    }
  }

  const homeRoot = document.getElementById('react-home');
  if (homeRoot) {
    const homeClips = parseData('home-feed-data');
    ReactDOM.createRoot(homeRoot).render(e(HomeFeed, { clips: homeClips }));
  }

  const myRoot = document.getElementById('react-myclips');
  if (myRoot) {
    const myClips = parseData('my-clips-data');
    ReactDOM.createRoot(myRoot).render(e(MyClips, { clips: myClips }));
  }
})();
