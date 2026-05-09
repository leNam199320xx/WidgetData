const apiBase = '/api/product-demo';

function el(tag, className, html) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (html) node.innerHTML = html;
  return node;
}

function renderCards(containerId, items, mapper) {
  const root = document.getElementById(containerId);
  root.innerHTML = '';
  items.forEach(item => root.appendChild(mapper(item)));
}

async function loadPage() {
  const res = await fetch(apiBase);
  if (!res.ok) throw new Error('Không tải được dữ liệu demo');
  const data = await res.json();

  document.getElementById('product-name').textContent = data.product.name;
  document.getElementById('product-tagline').textContent = data.product.tagline;
  document.getElementById('product-description').textContent = data.product.description;
  document.getElementById('product-cta').textContent = data.product.cta;

  renderCards('metrics', data.metrics, x => {
    const card = el('article', 'card');
    card.innerHTML = `<span class="value">${x.value}</span><span>${x.label}</span>`;
    return card;
  });

  renderCards('features', data.features, x => {
    const card = el('article', 'card');
    card.innerHTML = `<h3>${x.title}</h3><p>${x.description}</p>`;
    return card;
  });

  renderCards('plans', data.plans, x => {
    const card = el('article', 'card');
    card.innerHTML = `<h3>${x.name}</h3><strong>${x.price}</strong><p>${x.description}</p>`;
    return card;
  });

  renderCards('testimonials', data.testimonials, x => {
    const card = el('article', 'card');
    card.innerHTML = `<p>“${x.quote}”</p><small>${x.author}</small>`;
    return card;
  });
}

async function submitLead(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const result = document.getElementById('lead-result');

  const payload = Object.fromEntries(new FormData(form).entries());

  try {
    const res = await fetch(`${apiBase}/leads`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (!res.ok) {
      const body = await res.json().catch(() => ({}));
      const msg = body.title || body.error || 'Gửi đăng ký thất bại';
      throw new Error(msg);
    }

    const data = await res.json();
    result.className = 'result success';
    result.textContent = `${data.message} (Lead ID: ${data.leadId})`;
    form.reset();
  } catch (err) {
    result.className = 'result error';
    result.textContent = err.message;
  }
}

document.getElementById('lead-form').addEventListener('submit', submitLead);
loadPage().catch(err => {
  const result = document.getElementById('lead-result');
  result.className = 'result error';
  result.textContent = err.message;
});
