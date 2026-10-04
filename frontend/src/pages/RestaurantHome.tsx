import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { restaurantContact as contact, restaurantDishes } from './restaurantContent';
import './RestaurantHome.css';

const categories = ['Todo', 'Parrilla', 'Bebidas'];

export default function RestaurantHome() {
  const [category, setCategory] = useState('Todo');
  const [menuOpen, setMenuOpen] = useState(false);
  const hasContact = Boolean(contact.address || contact.hours || contact.whatsapp || contact.instagramUrl || contact.mapsUrl);

  useEffect(() => {
    const previousTitle = document.title;
    document.title = 'El Caldero Flameante | Magia, fuego y buena comida';
    return () => { document.title = previousTitle; };
  }, []);

  return (
    <div className="caldero-site" id="inicio">
      <a className="caldero-skip" href="#contenido">Saltar al contenido</a>
      <header className="caldero-header">
        <a href="#inicio" className="caldero-brand" aria-label="El Caldero Flameante, inicio">
          <img src="/restaurant/logo.png" alt="El Caldero Flameante — Fast Food and Grill" width="155" height="109" />
        </a>
        <button className="caldero-menu-toggle" type="button" aria-expanded={menuOpen} aria-controls="caldero-navigation" onClick={() => setMenuOpen(!menuOpen)}>{menuOpen ? 'Cerrar ✕' : 'Menú ☰'}</button>
        <nav id="caldero-navigation" className={menuOpen ? 'is-open' : ''} aria-label="Navegación principal">
          <a href="#sabores" onClick={() => setMenuOpen(false)}>Nuestros sabores</a>
          <a href="#experiencia" onClick={() => setMenuOpen(false)}>La experiencia</a>
          <a href="#elixires" onClick={() => setMenuOpen(false)}>Micheladas</a>
          {hasContact && <a href="#visitanos" onClick={() => setMenuOpen(false)}>Visítanos <span aria-hidden="true">↗</span></a>}
        </nav>
        <span className="caldero-header-note">FAST FOOD <i>&</i> GRILL</span>
      </header>

      <main id="contenido">
        <section className="caldero-hero" aria-labelledby="hero-title">
          <div className="caldero-hero-copy">
            <p className="caldero-eyebrow"><span aria-hidden="true">✦</span> BIENVENIDO A EL CALDERO FLAMEANTE</p>
            <h1 id="hero-title">Un poco de magia.<br />Mucho <em>sabor.</em></h1>
            <p className="caldero-intro">Hay lugares que te transportan. Y sabores que te hacen volver. Entra a nuestro mundo de fantasía, parrilla y buenos momentos.</p>
            <div className="caldero-actions">
              <a className="caldero-button" href="#sabores">Explorar los sabores <span aria-hidden="true">↗</span></a>
              <a className="caldero-text-link" href="#experiencia">Descubre el lugar <span aria-hidden="true">↓</span></a>
            </div>
            <div className="caldero-hero-foot"><span aria-hidden="true">✧</span> La aventura comienza en la mesa.</div>
          </div>
          <div className="caldero-hero-photo">
            <img src="/restaurant/costillas-retocadas.png" alt="Costillas con papas y ensalada en nuestro restaurante de ambientación mágica" width="1086" height="1448" fetchPriority="high" />
            <div className="caldero-photo-caption"><span>DEL FUEGO A TU MESA</span><strong>Un antojo de otro mundo.</strong></div>
            <div className="caldero-seal" aria-hidden="true">FUEGO<br /><span>✦</span><br />& FANTASÍA</div>
          </div>
        </section>

        <div className="caldero-ribbon" aria-hidden="true"><span>MAGIA EN EL AMBIENTE</span><b>✦</b><span>FUEGO EN LA PARRILLA</span><b>✦</b><span>BUENOS MOMENTOS EN LA MESA</span></div>

        <section className="caldero-section caldero-flavors" id="sabores" aria-labelledby="flavors-title">
          <div className="caldero-section-heading"><div><p className="caldero-eyebrow">EL GRIMORIO DE SABORES</p><h2 id="flavors-title">Déjate <em>tentar.</em></h2></div><p>Una primera mirada a lo que sale de nuestro caldero.<br />¿Con qué empieza tu aventura?</p></div>
          <div className="caldero-filters" role="group" aria-label="Filtrar selección de platos">{categories.map(item => <button type="button" key={item} aria-pressed={category === item} onClick={() => setCategory(item)}>{item}</button>)}</div>
          <div className="caldero-dishes" aria-live="polite">
            {restaurantDishes.filter(dish => category === 'Todo' || dish.category === category).map((dish, index) => <article className="caldero-dish" key={dish.name}>
              <div className="caldero-dish-photo"><img src={dish.image} alt={dish.name} loading="lazy" width="600" height="660" style={{ objectPosition: dish.position }} /><span>{dish.category}</span></div>
              <p className="caldero-dish-note">{dish.note}</p><h3>{dish.name}<span aria-hidden="true">0{index + 1}</span></h3><p>{dish.description}</p>
            </article>)}
          </div>
        </section>

        <section className="caldero-experience" id="experiencia" aria-labelledby="experience-title">
          <div className="caldero-experience-photo"><img src="/restaurant/micheladas.jpg" alt="Copas de micheladas frente a las paredes de piedra, mesas de madera y decoración del restaurante" loading="lazy" width="1600" height="1200" /><span className="caldero-photo-label">UN RINCÓN FUERA DE LO COTIDIANO</span></div>
          <div className="caldero-experience-copy"><p className="caldero-eyebrow">CRUZA EL UMBRAL</p><h2>Tu próxima historia<br />comienza <em>aquí.</em></h2><p>Muros de piedra, detalles de fantasía y una mesa esperando por ti. El Caldero Flameante es una invitación a dejar la rutina afuera y disfrutar el momento.</p><p>Ven por la comida. Quédate por la conversación, un brindis y ese pequeño instante de magia en buena compañía.</p><a href="#elixires" className="caldero-text-link">Un brindis por los buenos momentos <span aria-hidden="true">↗</span></a></div>
        </section>

        <section className="caldero-section caldero-drinks" id="elixires" aria-labelledby="drinks-title">
          <div><p className="caldero-eyebrow">POCIONES PARA BRINDAR</p><h2>¿De qué color<br />es tu <em>magia?</em></h2><p>Nuestras micheladas de las casas ponen el color sobre la mesa. El siguiente brindis puede ser el tuyo.</p><div className="caldero-colors" aria-hidden="true"><i /><i /><i /><i /><i /></div><span className="caldero-drinks-note">Micheladas de las casas · Bebidas con alcohol</span></div>
          <figure><img src="/restaurant/brindis.jpeg" alt="Micheladas de distintos colores servidas en copas con limón y adornos de frutas y gomitas" loading="lazy" width="960" height="1280" /><figcaption>Para encuentros que merecen recordarse. <span aria-hidden="true">✦</span></figcaption></figure>
        </section>

        <section className="caldero-visit" id="visitanos" aria-labelledby="visit-title"><p className="caldero-eyebrow">EL CALDERO FLAMEANTE</p><h2 id="visit-title">Te guardamos un lugar<br />en esta <em>historia.</em></h2>
          {hasContact ? (
            <div className="caldero-contact">
              {contact.address && <p>{contact.address}</p>}
              {contact.hours && <p>{contact.hours}</p>}
              <div className="caldero-actions">
                {contact.whatsapp && <a className="caldero-button" href={`https://wa.me/${contact.whatsapp}`} target="_blank" rel="noreferrer">Escríbenos por WhatsApp ↗</a>}
                {contact.mapsUrl && <a className="caldero-text-link" href={contact.mapsUrl} target="_blank" rel="noreferrer">Cómo llegar ↗</a>}
              </div>
              <div className="caldero-actions">
                {contact.instagramUrl && <a className="caldero-text-link" href={contact.instagramUrl} target="_blank" rel="noreferrer">Instagram ↗</a>}
                {contact.facebookUrl && <a className="caldero-text-link" href={contact.facebookUrl} target="_blank" rel="noreferrer">Facebook ↗</a>}
                {contact.tiktokUrl && <a className="caldero-text-link" href={contact.tiktokUrl} target="_blank" rel="noreferrer">TikTok ↗</a>}
              </div>
            </div>
          ) : <a className="caldero-button" href="#sabores">Encuentra tu próximo antojo <span aria-hidden="true">↑</span></a>}
        </section>
      </main>
      <footer className="caldero-footer"><a href="#inicio"><img src="/restaurant/logo.png" alt="El Caldero Flameante" width="130" height="91" loading="lazy" /></a><p>Magia, fuego y buena comida.<br /><span>© {new Date().getFullYear()} El Caldero Flameante</span></p><Link to="/login">Acceso del personal <span aria-hidden="true">↗</span></Link></footer>
    </div>
  );
}
