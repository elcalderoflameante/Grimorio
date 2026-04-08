import { useEffect, useState } from 'react';
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

const requestOptions = [
  { id: 'waiter', label: 'Llamar mesero', image: llamarMeseroImg },
  { id: 'bill', label: 'Pedir cuenta', image: pedirCuentaImg },
  { id: 'napkins', label: 'Servilletas', image: servilletasImg },
  { id: 'salt', label: 'Sal', image: salImg },
  { id: 'sauce', label: 'Salsa', image: salsaTomateImg },
  { id: 'mayo', label: 'Mayonesa', image: salsaMayonesa },
  { id: 'chili', label: 'Aji', image: salsaAji },
  { id: 'box', label: 'Contenedor', image: contenedorImg },
];

export default function PublicTableRequest() {
  const { token } = useParams<{ token: string }>();
  const [tableCode, setTableCode] = useState<string | null>(null);
  const [customRequest, setCustomRequest] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    const loadTableInfo = async () => {
      if (!token) {
        setTableCode(null);
        return;
      }

      try {
        const response = await tableServiceApi.getPublicTable(token);
        setTableCode(response.data.code || null);
      } catch {
        setTableCode(null);
      }
    };

    loadTableInfo().catch(() => {});
  }, [token]);

  const handleCustomRequest = async () => {
    if (!customRequest.trim() || !token) return;
    
    try {
      setIsSubmitting(true);
      // Aquí irá la llamada a la API cuando esté lista
      // await tableServiceApi.createPublicRequest(token, { type: 'custom', message: customRequest });
      setCustomRequest('');
      // Mostrar mensaje de éxito
    } catch {
      // Manejar error
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 h-[100dvh] w-screen overflow-hidden">
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
              <RusticButton key={option.id} className="h-[150px]">
                <div className="flex w-full flex-row items-center justify-start gap-4 px-3 text-[#3e2723]">
                  <img 
                    src={option.image} 
                    alt=""
                    className="h-40 w-40 object-contain"
                  />
                  <span className="text-left text-4xl font-bold leading-tight [font-family:'Eagle_Lake',serif]">
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
  );
}
