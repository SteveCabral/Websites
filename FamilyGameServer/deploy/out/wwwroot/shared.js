// Shared helpers for host/play pages.

export function qs(sel){ return document.querySelector(sel); }
export function qsa(sel){ return Array.from(document.querySelectorAll(sel)); }

export function setText(el, text){
  if (!el) return;
  el.textContent = text ?? "";
}

export function escapeHtml(text){
  const div = document.createElement('div');
  div.textContent = text ?? '';
  return div.innerHTML;
}

export function toRoomCode(raw){
  return (raw ?? '').trim().toUpperCase();
}

export function renderScoreboard(tableBodyEl, scoreboard){
  if (!tableBodyEl) return;
  tableBodyEl.innerHTML = '';

  for (let i = 0; i < (scoreboard?.length ?? 0); i++){
    const p = scoreboard[i];
    const tr = document.createElement('tr');

    const rank = document.createElement('td');
    rank.textContent = String(i + 1);

    const name = document.createElement('td');
    name.textContent = p?.name ?? p?.Name ?? '';

    const score = document.createElement('td');
    score.textContent = String(p?.score ?? p?.Score ?? 0);

    const answered = document.createElement('td');
    const a = p?.answered ?? p?.Answered;
    answered.textContent = a ? 'Yes' : 'No';

    tr.appendChild(rank);
    tr.appendChild(name);
    tr.appendChild(score);
    tr.appendChild(answered);
    tableBodyEl.appendChild(tr);
  }
}
