// Theme: explicit choice wins, otherwise the OS decides.
(function () {
  var stored = null;
  try { stored = localStorage.getItem('mudex-theme'); } catch (e) { /* private mode */ }
  if (stored === 'light' || stored === 'dark') document.documentElement.dataset.theme = stored;

  document.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-theme-toggle]');
    if (!btn) return;
    var isDark = document.documentElement.dataset.theme
      ? document.documentElement.dataset.theme === 'dark'
      : matchMedia('(prefers-color-scheme: dark)').matches;
    var next = isDark ? 'light' : 'dark';
    document.documentElement.dataset.theme = next;
    btn.setAttribute('aria-pressed', String(next === 'dark'));
    try { localStorage.setItem('mudex-theme', next); } catch (e) { /* ignore */ }
  });
})();

// Scroll reveal
(function () {
  var items = document.querySelectorAll('.reveal');
  if (!items.length) return;
  if (!('IntersectionObserver' in window) || matchMedia('(prefers-reduced-motion: reduce)').matches) {
    items.forEach(function (el) { el.classList.add('in'); });
    return;
  }
  var io = new IntersectionObserver(function (entries) {
    entries.forEach(function (entry, i) {
      if (!entry.isIntersecting) return;
      setTimeout(function () { entry.target.classList.add('in'); }, i * 60);
      io.unobserve(entry.target);
    });
  }, { rootMargin: '0px 0px -10% 0px' });
  items.forEach(function (el) { io.observe(el); });
})();

// Table of contents highlight
(function () {
  var links = document.querySelectorAll('.toc a[href^="#"]');
  if (!links.length) return;
  var map = {};
  var targets = [];
  links.forEach(function (a) {
    var el = document.getElementById(decodeURIComponent(a.hash.slice(1)));
    if (el) { map[el.id] = a; targets.push(el); }
  });
  var io = new IntersectionObserver(function (entries) {
    entries.forEach(function (entry) {
      if (!entry.isIntersecting) return;
      links.forEach(function (a) { a.classList.remove('active'); });
      map[entry.target.id].classList.add('active');
    });
  }, { rootMargin: '-80px 0px -70% 0px' });
  targets.forEach(function (el) { io.observe(el); });
})();

// Draw the gilde mark once, a beat after the footer comes into view
(function () {
  var mark = document.querySelector('.gilde-mark');
  if (!mark || !('IntersectionObserver' in window)) return;
  if (matchMedia('(prefers-reduced-motion: reduce)').matches) return;
  var io = new IntersectionObserver(function (entries) {
    if (!entries[0].isIntersecting) return;
    io.disconnect();
    setTimeout(function () { mark.classList.add('draw'); }, 1000);
  }, { threshold: .4 });
  io.observe(mark);
})();
