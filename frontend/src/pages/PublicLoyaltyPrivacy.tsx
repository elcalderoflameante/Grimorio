import { Link } from 'react-router-dom';
import { Button, Typography } from 'antd';
import { WhatsAppOutlined } from '@ant-design/icons';
import { restaurantContact } from './restaurantContent';

export default function PublicLoyaltyPrivacy({ deletion = false }: { deletion?: boolean }) {
  return <main style={{ maxWidth: 680, margin: '0 auto', padding: '28px 20px 48px', minHeight: '100vh', overflowWrap: 'anywhere' }}>
    <img src="/restaurant/logo.png" width={112} height={79} alt="El Caldero Flameante" style={{ objectFit: 'contain' }} />
    <Typography.Title level={3}>El Caldero Flameante</Typography.Title>
    <Typography.Title level={4}>{deletion ? 'Solicitud de eliminacion de datos' : 'Privacidad del programa de recompensas'}</Typography.Title>
    {deletion ? <>
      <p>Puedes solicitar la eliminacion de tu ficha del programa en caja o por el WhatsApp del restaurante. Indica que deseas eliminar tu tarjeta de fidelizacion y el telefono con el que te afiliaste.</p>
      <p>Confirmaremos que eres titular del numero antes de atender la solicitud. No envies contrasenas, codigos de verificacion, tarjetas bancarias ni fotografias de documentos.</p>
      <p>Te informaremos del cierre de la tarjeta, de los datos eliminados o anonimizados y de cualquier registro que deba conservarse por obligaciones aplicables. Los documentos fiscales y las operaciones de venta se administran por separado.</p>
      <p>La solicitud no elimina por si sola el historial de ventas, facturas ni los registros necesarios para atender reclamaciones. Los premios pendientes se revisaran al tramitar el cierre.</p>
    </> : <>
      <p>El Caldero Flameante administra los datos de su programa de recompensas para identificar tu tarjeta, verificar su acceso y registrar consumos participantes, visitas y premios.</p>
      <h2 style={{ fontSize: 18 }}>Datos que utilizamos</h2>
      <p>Nombre y celular; opcionalmente correo electronico y dia/mes de cumpleanos, sin anio. Registramos la aceptacion del programa, tu preferencia de comunicaciones comerciales y la actividad de tu tarjeta. No necesitas cedula o RUC para afiliarte.</p>
      <h2 style={{ fontSize: 18 }}>Verificacion y comunicaciones</h2>
      <p>Cuando solicitas acceso, enviamos un codigo por WhatsApp mediante la plataforma de Meta. Meta procesa el numero y el mensaje para prestarnos ese servicio segun sus propias condiciones. No compartas tu codigo ni el enlace privado de tu tarjeta.</p>
      <p>Recibir promociones es opcional y tiene una autorizacion separada. Puedes retirarla en caja o por WhatsApp sin renunciar a tu tarjeta. El cumpleanos no activa mensajes promocionales por si solo.</p>
      <h2 style={{ fontSize: 18 }}>Conservacion y solicitudes</h2>
      <p>Conservamos la ficha y el historial mientras sean necesarios para administrar tu participacion. Las solicitudes de verificacion se depuran a partir de siete dias; el codigo no se guarda en claro. Los registros de venta y facturacion tienen su propio tratamiento.</p>
      <p>Puedes consultar, corregir o solicitar el cierre y eliminacion de tu ficha en caja o por WhatsApp. Verificaremos tu identidad antes de modificar datos. No vendemos tus datos ni publicamos tu celular, correo o cumpleanos en la tarjeta.</p>
      <p><Link to="/fidelizacion/eliminar-datos">Solicitar eliminacion de datos</Link></p>
    </>}
    <Button icon={<WhatsAppOutlined />} href={`https://wa.me/${restaurantContact.whatsapp}?text=${encodeURIComponent(deletion ? 'Quiero solicitar la eliminacion de mi ficha de fidelizacion.' : 'Tengo una consulta sobre mis datos del programa de recompensas.')}`}
      target="_blank" rel="noopener noreferrer">Contactar al restaurante</Button>
    <p style={{ marginTop: 24 }}><Link to="/fidelizacion">Volver al programa</Link></p>
  </main>;
}
