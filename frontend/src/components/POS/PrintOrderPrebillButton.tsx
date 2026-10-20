import { useRef, useState } from 'react';
import { App, Button, Tooltip } from 'antd';
import { PrinterOutlined } from '@ant-design/icons';
import type { CustomerDto } from '../../types';
import { posApi } from '../../services/api';
import { formatError } from '../../utils/errorHandler';
import { openThermalPrintWindow, printThermalPrebill } from '../../utils/thermalReceiptPrinter';

export default function PrintOrderPrebillButton({ orderId, customer, disabledReason }: {
  orderId: string; customer?: CustomerDto | null; disabledReason?: string;
}) {
  const { message } = App.useApp();
  const [printing, setPrinting] = useState(false);
  const inFlight = useRef(false);
  const print = async () => {
    if (inFlight.current || disabledReason) return;
    let printWindow: Window | undefined;
    inFlight.current = true;
    setPrinting(true);
    try {
      // Open synchronously from the click so the browser does not block the popup.
      printWindow = openThermalPrintWindow();
      printWindow.document.title = 'Precuenta';
      if (printWindow.document.body) printWindow.document.body.textContent = 'Preparando precuenta...';
      const { data } = await posApi.getPrebill(orderId);
      printThermalPrebill(customer ? { ...data, customer } : data, printWindow);
    } catch (error) {
      if (printWindow && !printWindow.closed) printWindow.close();
      message.error(formatError(error));
    } finally {
      inFlight.current = false;
      setPrinting(false);
    }
  };
  return <Tooltip title={disabledReason ?? 'Imprimir precuenta sin registrar un cobro'}>
    <span style={{ display: 'block' }}><Button block aria-label="Imprimir cuenta" icon={<PrinterOutlined />} loading={printing}
      disabled={!!disabledReason} onClick={print}>Imprimir cuenta</Button></span>
  </Tooltip>;
}
