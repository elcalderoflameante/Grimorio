import { useEffect, useRef, useState } from 'react';
import { Alert, App, Button, Input, Modal, QRCode, Space, Spin, Tag, Tooltip } from 'antd';
import { CopyOutlined, DownloadOutlined, ExportOutlined, ReloadOutlined } from '@ant-design/icons';
import { publicLoyaltyApi } from '../../services/publicLoyaltyApi';
import type { PublicLoyaltyInfo } from '../../services/publicLoyaltyApi';
import { isLocalLoyaltyUrl, loyaltyEnrollmentUrl } from '../../utils/loyaltyPresentation';

export default function LoyaltyEnrollmentQr({ onClose }: { onClose: () => void }) {
  const { message } = App.useApp();
  const qr = useRef<HTMLDivElement>(null);
  const [info, setInfo] = useState<PublicLoyaltyInfo>();
  const [failed, setFailed] = useState(false);
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState(false);
  const [reload, setReload] = useState(0);
  const url = loyaltyEnrollmentUrl(import.meta.env.VITE_PUBLIC_APP_URL as string | undefined, window.location.origin);
  useEffect(() => {
    let current = true;
    void publicLoyaltyApi.info().then(r => { if (current) { setInfo(r.data); setFailed(false); } })
      .catch(() => { if (current) { setInfo(undefined); setFailed(true); } })
      .finally(() => { if (current) setLoading(false); });
    return () => { current = false; };
  }, [reload]);

  async function download() {
    const svg = qr.current?.querySelector('svg');
    if (!svg || !url) return;
    setDownloading(true);
    let blobUrl: string | undefined;
    try {
      const copy = svg.cloneNode(true) as SVGSVGElement;
      copy.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
      copy.setAttribute('width', '768'); copy.setAttribute('height', '768');
      blobUrl = URL.createObjectURL(new Blob([new XMLSerializer().serializeToString(copy)], { type: 'image/svg+xml;charset=utf-8' }));
      const image = new Image(); image.src = blobUrl; await image.decode();
      const canvas = document.createElement('canvas'); canvas.width = 1024; canvas.height = 1024;
      const context = canvas.getContext('2d');
      if (!context) throw new Error('Canvas unavailable');
      context.fillStyle = '#ffffff'; context.fillRect(0, 0, 1024, 1024);
      context.imageSmoothingEnabled = false;
      // Keep an opaque quiet zone around the code, without a logo covering its modules.
      context.drawImage(image, 128, 128, 768, 768);
      const anchor = document.createElement('a'); anchor.href = canvas.toDataURL('image/png');
      anchor.download = 'caldero-flameante-afiliacion-qr.png'; anchor.click();
    } catch { message.error('No se pudo descargar el QR. Intenta nuevamente.'); }
    finally { if (blobUrl) URL.revokeObjectURL(blobUrl); setDownloading(false); }
  }

  return <Modal title="QR de afiliacion" open onCancel={onClose} footer={null} width={480}>
    <Space orientation="vertical" size={16} style={{ width: '100%' }}>
      <Spin spinning={loading}>
        {failed ? <Alert type="warning" showIcon title="No se pudo comprobar la disponibilidad" />
          : info && (!info.enabled ? <Alert type="warning" showIcon title="Afiliacion en linea desactivada" description="Pendiente de habilitacion antes de compartir el QR con los clientes." />
            : <Tag color={info.testMode ? 'blue' : 'green'}>{info.testMode ? 'En pruebas: solo telefonos autorizados' : 'Afiliacion disponible'}</Tag>)}
      </Spin>
      {!url ? <Alert type="error" showIcon title="La URL publica no es valida" description="Revisa VITE_PUBLIC_APP_URL antes de generar el QR." /> : <>
        {isLocalLoyaltyUrl(url) && <Alert type="info" showIcon title="Enlace local de pruebas" description="No compartir en redes ni imprimir para el publico." />}
        <div ref={qr} style={{ display: 'flex', justifyContent: 'center', background: '#fff', padding: 16 }}>
          <QRCode value={url} type="svg" size={224} errorLevel="M" bordered={false} color="#000000" bgColor="#ffffff" />
        </div>
        <Input readOnly aria-label="Enlace de afiliacion" value={url} onFocus={e => e.target.select()} />
        <Space wrap>
          <Button type="primary" icon={<DownloadOutlined />} loading={downloading} onClick={() => void download()}>Descargar QR</Button>
          <Tooltip title="Copiar enlace de afiliacion"><Button aria-label="Copiar enlace de afiliacion" icon={<CopyOutlined />} onClick={() => {
            if (!navigator.clipboard) { message.info('Selecciona el enlace para copiarlo'); return; }
            void navigator.clipboard.writeText(url).then(() => message.success('Enlace copiado')).catch(() => message.error('No se pudo copiar el enlace'));
          }} /></Tooltip>
          <Tooltip title="Abrir afiliacion"><Button aria-label="Abrir afiliacion" icon={<ExportOutlined />} href={url} target="_blank" rel="noopener noreferrer" /></Tooltip>
          <Tooltip title="Actualizar disponibilidad"><Button aria-label="Actualizar disponibilidad" icon={<ReloadOutlined />} loading={loading}
            onClick={() => { setLoading(true); setReload(n => n + 1); }} /></Tooltip>
        </Space>
      </>}
    </Space>
  </Modal>;
}
