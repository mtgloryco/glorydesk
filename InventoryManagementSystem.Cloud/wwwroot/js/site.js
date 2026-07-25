/* Glory Desk portal — shared scripts */

function setActiveNav() {
  const path = window.location.pathname.replace(/\/$/, '') || '/';
  document.querySelectorAll('.main-nav a[data-nav]').forEach(a => {
    const href = (a.getAttribute('href') || '').replace(/\/$/, '') || '/';
    const match = href === path || (href !== '/' && path.startsWith(href));
    if (match) a.classList.add('active');
  });
}

async function api(path, options = {}) {
  const res = await fetch(path, {
    headers: { 'Content-Type': 'application/json', ...(options.headers || {}) },
    ...options
  });
  const data = await res.json().catch(() => ({}));
  return { ok: res.ok, status: res.status, data };
}

function getToken() {
  return localStorage.getItem('gd_token') || '';
}

function setToken(token) {
  if (token) localStorage.setItem('gd_token', token);
  else localStorage.removeItem('gd_token');
}

function authHeaders() {
  const t = getToken();
  return t ? { Authorization: `Bearer ${t}` } : {};
}

document.addEventListener('DOMContentLoaded', setActiveNav);
