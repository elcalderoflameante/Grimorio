import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import fondoPergamino from '../assets/fondo-pergamino.jpg';
import ecfLogo from '../assets/ECF-Logo.png';
import llamarMeseroImg from '../assets/llamar-mesero.png';
import pedirCuentaImg from '../assets/pedir-cuenta.png';
import servilletasImg from '../assets/servilletas.png';
import salImg from '../assets/sal.png';
import salsaTomateImg from '../assets/salsa-tomate.png';
import salsaMayonesa from '../assets/salsa-mayonesa.png';
import salsaAji from '../assets/salsa-aji.png';
import contenedorImg from '../assets/contenedor.png';
import './PublicTableRequest.tailwind.pcss';
import { tableServiceApi } from '../services/api';
import RusticButton from '../components/Public/RusticButton';
import type { TableServiceRequestType } from '../types';
import Lottie from 'lottie-react';
import magicAnimation from '../assets/magic-animation.json';

// Status: Pending=1 Taken=2 InProgress=3 Completed=4 Cancelled=5
const ACTIVE_STATUSES = new Set([1, 2, 3]);

const STATUS_MESSAGES: Record<number, { title: string; subtitle: string }> = {
  1: {
    title: '¡Solicitud enviada!',
    subtitle: 'Pronto un mago atenderá tu solicitud...',
  },
  2: {
    title: 'Un mago aceptó tu solicitud',
    subtitle: 'Ya está en camino a tu mesa.',
  },
  3: {
    title: 'El mago está atendiendo tu mesa',
    subtitle: 'En breve estaremos contigo.',
  },
};

const requestOptions = [
  { id: 'waiter', label: 'Llamar mesero', image: llamarMeseroImg, type: 8 as TableServiceRequestType },
  { id: 'bill', label: 'Pedir cuenta', image: pedirCuentaImg, type: 7 as TableServiceRequestType },
  { id: 'napkins', label: 'Servilletas', image: servilletasImg, type: 1 as TableServiceRequestType },
  { id: 'salt', label: 'Sal', image: salImg, type: 2 as TableServiceRequestType },
  { id: 'sauce', label: 'Salsa de tomate', image: salsaTomateImg, type: 3 as TableServiceRequestType },
  { id: 'mayo', label: 'Mayonesa', image: salsaMayonesa, type: 4 as TableServiceRequestType },
  { id: 'chili', label: 'Ají', image: salsaAji, type: 5 as TableServiceRequestType },
  { id: 'box', label: 'Contenedor', image: contenedorImg, type: 6 as TableServiceRequestType },
];

interface PendingRequest {
  type: TableServiceRequestType;
  label: string;
  customMessage?: string;
}

export default function PublicTableRequest() {
  const { token } = useParams<{ token: string }>();
  const [tableCode, setTableCode] = useState<string | null>(null);
  const [tableId, setTableId] = useState<string | null>(null);
  const [customRequest, setCustomRequest] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [pendingRequest, setPendingRequest] = useState<PendingRequest | null>(null);
  const [activeRequestId, setActiveRequestId] = useState<string | null>(null);
  const [activeRequestStatus, setActiveRequestStatus] = useState<number | null>(null);
  const connectionRef = useRef<HubConnection | null>(null);
  const activeRequestIdRef = useRef<string | null>(null);

  useEffect(() => {
    activeRequestIdRef.current = activeRequestId;
  }, [activeRequestId]);

  useEffect(() => {
    const loadTableInfo = async () => {
      if (!token) {
        setTableCode(null);
        setTableId(null);
        setActiveRequestId(null);
        setActiveRequestStatus(null);
        return;
      }

      try {
        const response = await tableServiceApi.getPublicTable(token);
        setTableCode(response.data.code || null);

        const resolvedTableId = response.data.tableId || null;
        setTableId(resolvedTableId);

        const activeRequestResponse = await tableServiceApi.getPublicActiveRequest(token);
        const activeRequest = activeRequestResponse.data;

        if (activeRequest && ACTIVE_STATUSES.has(activeRequest.status)) {
          setActiveRequestId(activeRequest.id);
          setActiveRequestStatus(activeRequest.status);
        } else {
          setActiveRequestId(null);
          setActiveRequestStatus(null);
        }
      } catch {
        setTableCode(null);
        setTableId(null);
        setActiveRequestId(null);
        setActiveRequestStatus(null);
      }
    };

    loadTableInfo().catch(() => {});
  }, [token]);

  useEffect(() => {
    if (!tableId) return;

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/table-service')
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    connection.on('tableService:request-updated', (request: { id: string; status: number; restaurantTableId: string }) => {
      if (!request || request.restaurantTableId !== tableId) return;

      if (ACTIVE_STATUSES.has(request.status)) {
        setActiveRequestId(request.id);
        setActiveRequestStatus(request.status);
        return;
      }

      if (activeRequestIdRef.current === request.id) {
        setActiveRequestStatus(request.status);
        setTimeout(() => {
          setActiveRequestId(null);
          setActiveRequestStatus(null);
        }, 2000);
      }
    });

    const startConnection = async () => {
      try {
        await connection.start();
        await connection.invoke('JoinPublicTable', tableId);
      } catch {
        // No-op: si falla, la vista aún muestra estado inicial por API
      }
    };

    connection.onreconnected(async () => {
      try {
        await connection.invoke('JoinPublicTable', tableId);
      } catch {
        // no-op
      }
    });

    startConnection().catch(() => {});

    return () => {
      connection.stop().catch(() => {});
      connectionRef.current = null;
    };
  }, [tableId]);

  const sendRequestAndTrack = async (type: TableServiceRequestType, customMessage?: string) => {
    if (!token || isSubmitting) return;

    try {
      setIsSubmitting(true);

      const response = await tableServiceApi.createPublicRequest({
        tableToken: token,
        type,
        customMessage,
      });

      const requestId = response.data.id;
      setActiveRequestId(requestId);
      setActiveRequestStatus(response.data.status);
    } catch {
      // No-op: la mascara es el feedback principal de estado
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOptionRequest = (type: TableServiceRequestType, label: string) => {
    setPendingRequest({ type, label });
  };

  const handleCustomRequest = () => {
    const message = customRequest.trim();
    if (!message) return;
    setPendingRequest({ type: 99, label: 'Solicitud Personalizada', customMessage: message });
  };

  const confirmRequest = async () => {
    if (!pendingRequest) return;
    await sendRequestAndTrack(pendingRequest.type, pendingRequest.customMessage);
    if (pendingRequest.type === 99) setCustomRequest('');
    setPendingRequest(null);
  };

  const cancelRequest = () => {
    setPendingRequest(null);
  };

  return (
    <>
      <div className="fixed inset-0 h-[100dvh] w-screen overflow-hidden">
      {/* Overlay de estado de solicitud activa */}
      {activeRequestId && activeRequestStatus && (
        <div className="absolute inset-0 z-40 overflow-hidden">
          <div className="absolute inset-0 z-0 bg-black" />

          <Lottie
            animationData={magicAnimation}
            loop
            className="absolute inset-0 z-10 h-full w-full"
          />

          <div className="absolute inset-x-0 top-0 z-30 flex justify-center px-6 pt-6">
            <img
              src={ecfLogo}
              alt="El Caldero Flameante"
              className="h-auto w-[74vw] max-w-[420px] min-w-[220px] drop-shadow-[0_12px_30px_rgba(0,0,0,0.7)]"
            />
          </div>

          <div className="absolute inset-x-0 bottom-0 z-30 px-6 pb-14">
            <div
              className="mx-auto max-w-[820px] rounded-2xl border-4 border-[#8B5E3C] px-8 py-6 text-center shadow-[inset_0_2px_4px_rgba(255,255,255,0.25),_inset_0_-2px_4px_rgba(0,0,0,0.2),_0_12px_30px_rgba(0,0,0,0.6)]"
              style={{ backgroundImage: 'url(' + fondoPergamino + ')', backgroundSize: 'cover', backgroundPosition: 'center' }}
            >
            {ACTIVE_STATUSES.has(activeRequestStatus) ? (
              <>
                <p className="text-[40px] font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif] drop-shadow-sm">
                  {STATUS_MESSAGES[activeRequestStatus]?.title}
                </p>
                <p className="mt-3 text-[30px] italic text-[#5d4037] [font-family:'Eagle_Lake',serif]">
                  {STATUS_MESSAGES[activeRequestStatus]?.subtitle}
                </p>
              </>
            ) : (
              <p className="text-[38px] font-bold text-[#4E7D40] [font-family:'Eagle_Lake',serif]">
                ¡Solicitud completada! ✨
              </p>
            )}
            </div>
          </div>
        </div>
      )}
      <img
        src={fondoPergamino}
        alt=""
        aria-hidden="true"
        className="absolute inset-0 h-full w-full object-cover"
      />

      <div className="relative z-10 h-full overflow-y-auto px-2 pb-6 pt-6">
        <div className="mx-auto flex w-full max-w-[900px] flex-col items-center">
          <div className="pointer-events-none flex w-full flex-col items-center">
            <img
              src={ecfLogo}
              alt="El Caldero Flameante"
              className="h-auto w-[88vw] max-w-[460px] min-w-[290px] drop-shadow-xl"
            />
            <h1 className="mt-0 text-center text-[36px] font-bold tracking-wide text-[#3e2723] [font-family:'Eagle_Lake',serif] drop-shadow-sm">
              Solicitud de Mesa {tableCode ?? '--'}
            </h1>
          </div>

          <div className="mt-5 grid w-full grid-cols-2 gap-2">
            {requestOptions.map((option) => (
              <RusticButton
                key={option.id}
                className="h-[150px]"
                onClick={() => handleOptionRequest(option.type, option.label)}
              >
                <div className="flex w-full flex-row items-center gap-4 px-3 text-[#3e2723]">
                  <img 
                    src={option.image} 
                    alt=""
                    className="h-40 w-40 object-contain"
                  />
                  <span className="flex-1 text-center text-4xl font-bold leading-tight [font-family:'Eagle_Lake',serif]">
                    {option.label}
                  </span>
                </div>
              </RusticButton>
            ))}
          </div>

          <div className="mt-8 w-full max-w-[900px]">
            <h2 className="text-center text-[28px] font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif]">
              Solicitud Personalizada
            </h2>
            <textarea
              value={customRequest}
              onChange={(e) => setCustomRequest(e.target.value)}
              placeholder="Escribe tu solicitud aquí..."
              className="mt-4 w-full box-border rounded-lg border-4 border-[#8B5E3C] bg-[#f5f1ed] p-6 text-[32px] font-semibold text-[#3e2723] placeholder-[#a1887f] focus:outline-none focus:ring-2 focus:ring-[#bc955c] resize-none"
              rows={6}
            />
            <button
              onClick={handleCustomRequest}
              disabled={isSubmitting || !customRequest.trim()}
              className="mt-4 h-[150px] w-full box-border flex items-center justify-center rounded-lg border-4 border-[#8B5E3C] bg-[#D2B48C] px-6 text-[24px] font-bold text-[#3e2723] drop-shadow-md transition-transform [font-family:'Eagle_Lake',serif] hover:scale-105 active:scale-95 disabled:opacity-50 disabled:hover:scale-100"
            >
              {isSubmitting ? 'Enviando...' : 'Enviar Solicitud'}
            </button>
          </div>
        </div>
      </div>
    </div>

      {pendingRequest && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
          <div
            className="w-full max-w-[680px] rounded-2xl border-4 border-[#8B5E3C] bg-[#f5ead8] p-10 shadow-2xl"
            style={{ backgroundImage: 'url(' + fondoPergamino + ')', backgroundSize: 'cover' }}
          >
            <h3 className="text-center text-[38px] font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif]">
              ¿Confirmar solicitud?
            </h3>
            <p className="mt-4 text-center text-[30px] text-[#5d4037] [font-family:'Eagle_Lake',serif]">
              {pendingRequest.label}
            </p>
            {pendingRequest.customMessage && (
              <p className="mt-3 text-center text-[24px] italic text-[#7b5e47] [font-family:'Eagle_Lake',serif]">
                &ldquo;{pendingRequest.customMessage}&rdquo;
              </p>
            )}
            <div className="mt-10 flex gap-5">
              <button
                onClick={cancelRequest}
                className="flex-1 rounded-xl border-4 border-[#8B5E3C] bg-[#e8d9c0] py-6 text-[28px] font-bold text-[#5d4037] transition-transform [font-family:'Eagle_Lake',serif] hover:scale-105 active:scale-95"
              >
                Cancelar
              </button>
              <button
                onClick={() => { confirmRequest().catch(() => {}); }}
                disabled={isSubmitting}
                className="flex-1 rounded-xl border-4 border-[#5a3a1a] bg-[#8B5E3C] py-6 text-[28px] font-bold text-[#f5ead8] transition-transform [font-family:'Eagle_Lake',serif] hover:scale-105 active:scale-95 disabled:opacity-50"
              >
                {isSubmitting ? 'Enviando...' : 'Confirmar'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
