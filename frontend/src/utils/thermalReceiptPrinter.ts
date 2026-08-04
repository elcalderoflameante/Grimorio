import type { ThermalReceiptDto } from '../types';
import { formatBranchDateTime } from './branchTimeZone';

const money = (value?: number) => `$${(value ?? 0).toFixed(2)}`;

const qty = (value?: number) => {
  const n = value ?? 0;
  return Number.isInteger(n) ? n.toFixed(0) : n.toFixed(2);
};

const esc = (value?: string | number | null) =>
  String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');

const docLabel = (receipt: ThermalReceiptDto) => {
  if (receipt.documentType === 'Factura') return 'FACTURA';
  return 'NOTA DE VENTA';
};

const itemRows = (receipt: ThermalReceiptDto) =>
  receipt.items.map(item => `
    <div class="item">
      <div class="item-name">${esc(item.name)}</div>
      <div class="item-line">
        <span>${qty(item.quantity)} x ${money(item.unitPrice)}</span>
        <strong>${money(item.total)}</strong>
      </div>
    </div>
  `).join('');

const paymentRows = (receipt: ThermalReceiptDto) =>
  receipt.payments.map(line => `
    <div class="row">
      <span>${esc(line.methodName)}</span>
      <strong>${money(line.amountTendered - line.change)}</strong>
    </div>
  `).join('');

const electronicDocument = (receipt: ThermalReceiptDto) => {
  const doc = receipt.electronicDocument;
  if (!doc) return '';
  return `
    <div class="section">
      <div class="center strong">DOCUMENTO ELECTRONICO</div>
      <div class="row"><span>No.</span><strong>${esc(doc.number)}</strong></div>
      <div class="row"><span>Estado</span><strong>${esc(doc.status)}</strong></div>
      ${doc.authorizationNumber ? `<div>Autorizacion: ${esc(doc.authorizationNumber)}</div>` : ''}
      ${doc.authorizedAt ? `<div>Fecha aut.: ${esc(formatBranchDateTime(doc.authorizedAt))}</div>` : ''}
      <div class="small break-word">Clave: ${esc(doc.accessKey)}</div>
    </div>
  `;
};

const buildHtml = (receipt: ThermalReceiptDto) => `
<!doctype html>
<html>
  <head>
    <meta charset="utf-8" />
    <title>${esc(docLabel(receipt))} ${esc(receipt.orderNumber)}</title>
    <style>
      @page { size: 80mm auto; margin: 0; }
      * { box-sizing: border-box; }
      body {
        margin: 0;
        background: #fff;
        color: #000;
        font-family: "Courier New", Courier, monospace;
        font-size: 11px;
        line-height: 1.25;
      }
      .receipt {
        width: 80mm;
        padding: 4mm;
      }
      .center { text-align: center; }
      .strong { font-weight: 700; }
      .title {
        font-size: 14px;
        font-weight: 700;
        text-align: center;
        text-transform: uppercase;
      }
      .section {
        border-top: 1px dashed #000;
        margin-top: 6px;
        padding-top: 6px;
      }
      .row, .item-line {
        display: flex;
        justify-content: space-between;
        gap: 8px;
      }
      .item { margin-bottom: 5px; }
      .item-name { font-weight: 700; overflow-wrap: anywhere; }
      .small { font-size: 10px; }
      .break-word { overflow-wrap: anywhere; word-break: break-word; }
      @media print {
        body { width: 80mm; }
        .receipt { padding: 3mm; }
      }
    </style>
  </head>
  <body>
    <div class="receipt">
      <div class="title">${esc(receipt.issuer.tradeName || receipt.issuer.businessName)}</div>
      <div class="center">${esc(receipt.issuer.businessName)}</div>
      <div class="center">RUC: ${esc(receipt.issuer.ruc)}</div>
      <div class="center">${esc(receipt.issuer.address)}</div>
      ${receipt.issuer.phone ? `<div class="center">Tel: ${esc(receipt.issuer.phone)}</div>` : ''}
      <div class="section center strong">${esc(docLabel(receipt))}</div>
      <div class="row"><span>Orden</span><strong>#${esc(receipt.orderNumber)}</strong></div>
      ${receipt.tableCode ? `<div class="row"><span>Mesa</span><strong>${esc(receipt.tableCode)}</strong></div>` : ''}
      <div class="row"><span>Fecha</span><strong>${esc(formatBranchDateTime(receipt.paidAt))}</strong></div>
      ${receipt.cashRegisterName ? `<div class="row"><span>Caja</span><strong>${esc(receipt.cashRegisterName)}</strong></div>` : ''}
      ${receipt.cashierName ? `<div class="row"><span>Cajero</span><strong>${esc(receipt.cashierName)}</strong></div>` : ''}

      <div class="section">
        <div>Cliente: ${esc(receipt.customer.name)}</div>
        ${receipt.customer.taxId ? `<div>Identificacion: ${esc(receipt.customer.taxId)}</div>` : ''}
        ${receipt.customer.address ? `<div>Direccion: ${esc(receipt.customer.address)}</div>` : ''}
      </div>

      <div class="section">
        ${itemRows(receipt)}
      </div>

      <div class="section">
        <div class="row"><span>Subtotal</span><strong>${money(receipt.totals.subtotal)}</strong></div>
        ${receipt.totals.discount > 0 ? `<div class="row"><span>Descuento</span><strong>${money(receipt.totals.discount)}</strong></div>` : ''}
        ${receipt.totals.tax > 0 ? `<div class="row"><span>IVA</span><strong>${money(receipt.totals.tax)}</strong></div>` : ''}
        <div class="row strong"><span>TOTAL</span><strong>${money(receipt.totals.total)}</strong></div>
        <div class="row"><span>Recibido</span><strong>${money(receipt.totals.tendered)}</strong></div>
        <div class="row"><span>Cambio</span><strong>${money(receipt.totals.change)}</strong></div>
      </div>

      <div class="section">
        <div class="center strong">PAGO</div>
        ${paymentRows(receipt)}
      </div>

      ${electronicDocument(receipt)}

      <div class="section center">
        <div>Gracias por su compra</div>
      </div>
    </div>
    <script>
      window.addEventListener('load', () => {
        setTimeout(() => {
          window.focus();
          window.print();
        }, 250);
      });
    </script>
  </body>
</html>
`;

export const printThermalReceipt = (receipt: ThermalReceiptDto) => {
  const printWindow = window.open('', '_blank', 'width=420,height=720');
  if (!printWindow) {
    throw new Error('No se pudo abrir la ventana de impresion.');
  }

  printWindow.document.open();
  printWindow.document.write(buildHtml(receipt));
  printWindow.document.close();
};
