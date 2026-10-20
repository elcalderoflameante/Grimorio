import type { OrderPrebillDto, ThermalReceiptDto } from '../types';
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

const receiptStyles = `
      @page { size: auto; margin: 0; }
      * { box-sizing: border-box; }
      html { margin: 0; padding: 0; }
      body {
        margin: 0;
        background: #fff;
        color: #000;
        font-family: Arial, Helvetica, sans-serif;
        font-size: 12px;
        line-height: 1.35;
        font-variant-numeric: tabular-nums;
      }
      .receipt {
        /* The LR2000 uses 80mm paper with a 72mm printable area. */
        width: 72mm;
        max-width: 100%;
        padding: 2mm;
        overflow-wrap: anywhere;
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
        display: grid;
        grid-template-columns: minmax(0, 1fr) minmax(0, 1.7fr);
        column-gap: 2mm;
        align-items: start;
        break-inside: avoid;
      }
      .row > *, .item-line > * { min-width: 0; overflow-wrap: anywhere; }
      .row > :last-child, .item-line > :last-child { text-align: right; }
      .item-line { grid-template-columns: repeat(2, minmax(0, 1fr)); }
      .item { margin-bottom: 6px; break-inside: avoid; }
      .item-name { font-weight: 700; overflow-wrap: anywhere; }
      .small { font-size: 11px; }
      .break-word { overflow-wrap: anywhere; word-break: break-word; }
`;

const buildHtml = (receipt: ThermalReceiptDto) => `
<!doctype html>
<html>
  <head>
    <meta charset="utf-8" />
    <title>${esc(docLabel(receipt))} ${esc(receipt.orderNumber)}</title>
    <style>${receiptStyles}</style>
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

export const openThermalPrintWindow = () => {
  const printWindow = window.open('', '_blank', 'width=420,height=720');
  if (!printWindow) {
    throw new Error('No se pudo abrir la ventana de impresion.');
  }
  return printWindow;
};

export const printThermalReceipt = (receipt: ThermalReceiptDto) => {
  const printWindow = openThermalPrintWindow();
  printWindow.document.open();
  printWindow.document.write(buildHtml(receipt));
  printWindow.document.close();
};

const writeField = (label: string, value?: string, tall = false) => `
  <div class="write-field"><div>${esc(label)}</div><div class="write-line${tall ? ' tall' : ''}">${esc(value)}</div></div>`;

export const printThermalPrebill = (bill: OrderPrebillDto, printWindow: Window) => {
  if (printWindow.closed) return;
  const items = bill.items.map(item => `<div class="item">
    <div class="item-name">${esc(item.name)}</div>
    ${(item.modifierSelections ?? []).map(s => `<div class="small">${esc(s.groupName)}: ${s.quantity > 1 ? `${qty(s.quantity)} x ` : ''}${esc(s.optionName)}</div>`).join('')}
    <div class="item-line"><span>${qty(item.quantity)} x ${money(item.unitPrice)}</span><strong>${money(item.total)}</strong></div>
  </div>`).join('');
  printWindow.document.open();
  printWindow.document.write(`<!doctype html><html><head><meta charset="utf-8" />
    <title>PRECUENTA ${esc(bill.orderNumber)}</title><style>${receiptStyles}
      .write-field { margin-top: 3mm; break-inside: avoid; }
      .write-line { min-height: 8mm; border-bottom: 1px solid #000; padding-top: 1mm; }
      .write-line.tall { min-height: 12mm; }
      .handwritten { break-inside: avoid; }
    </style></head><body><div class="receipt">
    <div class="title">${esc(bill.issuer.tradeName || bill.issuer.businessName)}</div>
    <div class="center">${esc(bill.issuer.businessName)}</div>
    <div class="center">RUC: ${esc(bill.issuer.ruc)}</div>
    <div class="center">${esc(bill.issuer.address)}</div>
    ${bill.issuer.phone ? `<div class="center">Tel: ${esc(bill.issuer.phone)}</div>` : ''}
    <div class="section center strong">PRECUENTA</div>
    <div class="center small strong">NO ES COMPROBANTE DE VENTA</div>
    <div class="row"><span>Orden</span><strong>#${esc(bill.orderNumber)}</strong></div>
    ${bill.tableCode ? `<div class="row"><span>Mesa</span><strong>${esc(bill.tableCode)}</strong></div>` : ''}
    <div class="row"><span>Emitida</span><strong>${esc(formatBranchDateTime(bill.printedAt))}</strong></div>
    ${bill.waiterName ? `<div class="row"><span>Atendido por</span><strong>${esc(bill.waiterName)}</strong></div>` : ''}
    <div class="section">${items}</div>
    <div class="section">
      <div class="row"><span>Subtotal (imp. incluidos)</span><strong>${money(bill.subtotal)}</strong></div>
      ${bill.discount > 0 ? `<div class="row"><span>Descuentos</span><strong>-${money(bill.discount)}</strong></div>` : ''}
      <div class="row strong"><span>TOTAL PEDIDO</span><strong>${money(bill.total)}</strong></div>
      ${bill.tax > 0 ? `<div class="row small"><span>Impuestos incluidos</span><span>${money(bill.tax)}</span></div>` : ''}
      ${bill.paidAmount > 0 ? `<div class="row"><span>Abonos</span><strong>-${money(bill.paidAmount)}</strong></div>` : ''}
      <div class="row strong"><span>POR PAGAR</span><strong>${money(bill.pendingAmount)}</strong></div>
    </div>
    <div class="section handwritten">${writeField('Propina voluntaria ($)')}
      <div class="small">No incluida en el total.</div></div>
    <div class="section">
      <div class="center strong">DATOS PARA FACTURACION</div>
      ${writeField('Nombre / Razon social', bill.customer?.name, true)}
      ${writeField('Cedula / RUC', bill.customer?.taxId)}
      ${writeField('Direccion', bill.customer?.address, true)}
      ${writeField('Correo electronico', bill.customer?.email, true)}
      ${writeField('Telefono', bill.customer?.phone)}
    </div>
    <div class="section center small">Cuenta provisional. Valores al momento de emision.<br />Gracias por su visita.</div>
    </div><script>window.addEventListener('load', () => {
      setTimeout(() => { window.focus(); window.print(); }, 250);
    });</script></body></html>`);
  printWindow.document.close();
};
