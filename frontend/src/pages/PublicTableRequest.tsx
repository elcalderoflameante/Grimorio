import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import Lottie from 'lottie-react';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import contenedorImg from '../assets/contenedor.png';
import ecfLogo from '../assets/ECF-Logo.png';
import fondoPergamino from '../assets/fondo-pergamino.jpg';
import llamarMeseroImg from '../assets/llamar-mesero.png';
import magicAnimation from '../assets/magic-animation.json';
import pedirCuentaImg from '../assets/pedir-cuenta.png';
import salImg from '../assets/sal.png';
import salsaAji from '../assets/salsa-aji.png';
import salsaMayonesa from '../assets/salsa-mayonesa.png';
import salsaTomateImg from '../assets/salsa-tomate.png';
import servilletasImg from '../assets/servilletas.png';
import RusticButton from '../components/Public/RusticButton';
import { tableServiceApi } from '../services/api';
import type {
  CreateModifierSelectionDto,
  OrderDto,
  PublicMenuCategoryDto,
  PublicMenuItemDto,
  PublicMenuItemModifierOptionDto,
  TableServiceRequestType,
} from '../types';
import './PublicTableRequest.tailwind.pcss';

const ACTIVE_STATUSES = new Set([1, 2, 3]);

const STATUS_MESSAGES: Record<number, { title: string; subtitle: string }> = {
  1: {
    title: 'Solicitud enviada',
    subtitle: 'Pronto un mesero atendera tu solicitud.',
  },
  2: {
    title: 'Un mesero acepto tu solicitud',
    subtitle: 'Ya esta en camino a tu mesa.',
  },
  3: {
    title: 'El mesero esta atendiendo tu mesa',
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
  { id: 'chili', label: 'Aji', image: salsaAji, type: 5 as TableServiceRequestType },
  { id: 'box', label: 'Contenedor', image: contenedorImg, type: 6 as TableServiceRequestType },
];

interface PendingRequest {
  type: TableServiceRequestType;
  label: string;
  customMessage?: string;
}

interface CartLine {
  localId: string;
  menuItemId: string;
  itemName: string;
  quantity: number;
  unitPrice: number;
  notes?: string;
  modifierSelections: CreateModifierSelectionDto[];
  modifierLabels: string[];
}

interface ItemDraft {
  item: PublicMenuItemDto;
  quantity: number;
  notes: string;
  selectedOptions: Record<string, PublicMenuItemModifierOptionDto[]>;
  error?: string;
}

type PublicTab = 'requests' | 'menu' | 'order';

const money = (value: number) => `$${value.toFixed(2)}`;
const createLocalId = () => crypto.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`;

const ORDER_STATUS_LABELS: Record<string, { label: string; detail: string }> = {
  Draft: {
    label: 'Pendiente de confirmar',
    detail: 'El mesero revisara tu pedido antes de enviarlo a preparacion.',
  },
  Confirmed: {
    label: 'Confirmado',
    detail: 'Tu pedido ya fue confirmado por el mesero.',
  },
  InPreparation: {
    label: 'En preparacion',
    detail: 'Cocina ya esta preparando tu pedido.',
  },
  Ready: {
    label: 'Listo',
    detail: 'Tu pedido esta listo para entregar.',
  },
  Delivered: {
    label: 'Entregado',
    detail: 'Tu pedido ya fue entregado.',
  },
  Cancelled: {
    label: 'Cancelado',
    detail: 'Este pedido fue cancelado.',
  },
};

export default function PublicTableRequest() {
  const { token } = useParams<{ token: string }>();
  const [tableCode, setTableCode] = useState<string | null>(null);
  const [tableId, setTableId] = useState<string | null>(null);
  const [customRequest, setCustomRequest] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [pendingRequest, setPendingRequest] = useState<PendingRequest | null>(null);
  const [activeRequestId, setActiveRequestId] = useState<string | null>(null);
  const [activeRequestStatus, setActiveRequestStatus] = useState<number | null>(null);
  const [tab, setTab] = useState<PublicTab>('requests');
  const [categories, setCategories] = useState<PublicMenuCategoryDto[]>([]);
  const [menuItems, setMenuItems] = useState<PublicMenuItemDto[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null);
  const [cart, setCart] = useState<CartLine[]>([]);
  const [itemDraft, setItemDraft] = useState<ItemDraft | null>(null);
  const [orderMessage, setOrderMessage] = useState<string | null>(null);
  const [menuError, setMenuError] = useState<string | null>(null);
  const [loadingMenu, setLoadingMenu] = useState(false);
  const [activeOrder, setActiveOrder] = useState<OrderDto | null>(null);
  const [orderError, setOrderError] = useState<string | null>(null);
  const [loadingOrder, setLoadingOrder] = useState(false);
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
    if (!token) return;

    const loadMenu = async () => {
      try {
        setLoadingMenu(true);
        setMenuError(null);
        const response = await tableServiceApi.getPublicTableMenu(token);
        setCategories(response.data.categories);
        setMenuItems(response.data.items);
        setSelectedCategoryId(response.data.categories[0]?.id ?? null);
      } catch {
        setMenuError('No se pudo cargar el menu en este momento.');
      } finally {
        setLoadingMenu(false);
      }
    };

    loadMenu().catch(() => {});
  }, [token]);

  const loadActiveOrder = useCallback(async (showLoading = false) => {
    if (!token) {
      setActiveOrder(null);
      return;
    }

    try {
      if (showLoading) setLoadingOrder(true);
      setOrderError(null);
      const response = await tableServiceApi.getActivePublicTableOrder(token);
      setActiveOrder(response.data);
    } catch {
      setOrderError('No se pudo cargar el pedido en este momento.');
    } finally {
      if (showLoading) setLoadingOrder(false);
    }
  }, [token]);

  useEffect(() => {
    loadActiveOrder(true).catch(() => {});
  }, [loadActiveOrder]);

  useEffect(() => {
    if (!token) return;

    const intervalId = window.setInterval(() => {
      loadActiveOrder().catch(() => {});
    }, 15000);

    return () => window.clearInterval(intervalId);
  }, [loadActiveOrder, token]);

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
        // No-op: si falla, la vista aun funciona por API.
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

  const visibleItems = useMemo(
    () => menuItems.filter(item => !selectedCategoryId || item.menuCategoryId === selectedCategoryId),
    [menuItems, selectedCategoryId],
  );

  const cartTotal = useMemo(
    () => cart.reduce((sum, line) => sum + line.unitPrice * line.quantity, 0),
    [cart],
  );

  const cartItemsCount = useMemo(
    () => cart.reduce((sum, line) => sum + line.quantity, 0),
    [cart],
  );

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
      // No-op: la mascara es el feedback principal de estado.
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
    setPendingRequest({ type: 99, label: 'Solicitud personalizada', customMessage: message });
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

  const openItemDraft = (item: PublicMenuItemDto) => {
    if (!item.isAvailable) return;
    setItemDraft({
      item,
      quantity: 1,
      notes: '',
      selectedOptions: {},
    });
  };

  const toggleModifierOption = (groupId: string, option: PublicMenuItemModifierOptionDto) => {
    if (!itemDraft || !option.isAvailable) return;
    const group = itemDraft.item.modifierGroups.find(g => g.id === groupId);
    if (!group) return;

    const current = itemDraft.selectedOptions[groupId] ?? [];
    const exists = current.some(o => o.id === option.id);
    let next: PublicMenuItemModifierOptionDto[];
    if (exists) {
      next = current.filter(o => o.id !== option.id);
    } else if (group.maxSelections <= 1) {
      next = [option];
    } else {
      next = current.length >= group.maxSelections ? current : [...current, option];
    }

    setItemDraft({
      ...itemDraft,
      selectedOptions: { ...itemDraft.selectedOptions, [groupId]: next },
      error: undefined,
    });
  };

  const addDraftToCart = () => {
    if (!itemDraft) return;
    const { item, selectedOptions } = itemDraft;

    for (const group of item.modifierGroups) {
      const selectedCount = (selectedOptions[group.id] ?? []).length;
      const minSelections = group.isRequired && group.minSelections === 0 ? 1 : group.minSelections;
      if (selectedCount < minSelections) {
        setItemDraft({ ...itemDraft, error: `Selecciona ${minSelections} opcion(es) en ${group.name}.` });
        return;
      }
      if (selectedCount > group.maxSelections) {
        setItemDraft({ ...itemDraft, error: `Solo puedes seleccionar hasta ${group.maxSelections} opcion(es) en ${group.name}.` });
        return;
      }
    }

    const selected = Object.values(selectedOptions).flat();
    const modifierTotal = selected.reduce((sum, option) => sum + option.priceDelta, 0);
    const modifierSelections = selected.map(option => ({
      modifierOptionId: option.id,
      quantity: 1,
    }));
    const modifierLabels = selected.map(option => `${option.name}${option.priceDelta ? ` (${money(option.priceDelta)})` : ''}`);

    setCart(prev => [
      ...prev,
      {
        localId: createLocalId(),
        menuItemId: item.id,
        itemName: item.name,
        quantity: itemDraft.quantity,
        unitPrice: item.price + modifierTotal,
        notes: itemDraft.notes.trim() || undefined,
        modifierSelections,
        modifierLabels,
      },
    ]);
    setItemDraft(null);
    setOrderMessage(null);
  };

  const updateCartQuantity = (localId: string, delta: number) => {
    setCart(prev => prev
      .map(line => line.localId === localId ? { ...line, quantity: line.quantity + delta } : line)
      .filter(line => line.quantity > 0));
  };

  const removeCartLine = (localId: string) => {
    setCart(prev => prev.filter(line => line.localId !== localId));
  };

  const submitDraftOrder = async () => {
    if (!token || cart.length === 0 || isSubmitting) return;

    try {
      setIsSubmitting(true);
      setOrderMessage(null);
      const response = await tableServiceApi.createPublicDraftOrder({
        tableToken: token,
        items: cart.map(line => ({
          menuItemId: line.menuItemId,
          quantity: line.quantity,
          notes: line.notes,
          modifierSelections: line.modifierSelections,
        })),
      });
      setCart([]);
      setActiveOrder(response.data.order);
      setTab('order');
      setOrderMessage(`Pedido #${response.data.order.number} enviado al mesero para confirmar.`);
    } catch (error: unknown) {
      const message = error && typeof error === 'object' && 'response' in error
        ? (error as { response?: { data?: { message?: string } } }).response?.data?.message
        : null;
      setOrderMessage(message || 'No se pudo enviar el pedido. Revisa disponibilidad e intenta otra vez.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      {activeRequestId && activeRequestStatus && (
        <div className="fixed inset-0 z-40 flex flex-col">
          <div className="absolute inset-0 bg-black" />
          <div className="relative z-10 mx-auto flex w-full max-w-sm flex-col sm:max-w-md" style={{ height: '100dvh' }}>
            <div className="flex justify-center pt-8 sm:pt-12">
              <img
                src={ecfLogo}
                alt="El Caldero Flameante"
                className="w-40 sm:w-52 md:w-60 drop-shadow-[0_8px_20px_rgba(0,0,0,0.7)]"
              />
            </div>
            <div className="flex-1">
              <Lottie animationData={magicAnimation} loop className="h-full w-full" />
            </div>
            <div className="px-4 pb-10 sm:pb-14">
              <div
                className="rounded-2xl border-4 border-[#8B5E3C] px-5 py-3 text-center shadow-xl"
                style={{ backgroundImage: `url(${fondoPergamino})`, backgroundSize: 'cover' }}
              >
                {ACTIVE_STATUSES.has(activeRequestStatus) ? (
                  <>
                    <p className="text-xl font-bold text-[#3e2723] sm:text-2xl [font-family:'Eagle_Lake',serif]">
                      {STATUS_MESSAGES[activeRequestStatus]?.title}
                    </p>
                    <p className="mt-2 text-base italic text-[#5d4037] sm:text-lg [font-family:'Eagle_Lake',serif]">
                      {STATUS_MESSAGES[activeRequestStatus]?.subtitle}
                    </p>
                  </>
                ) : (
                  <p className="text-xl font-bold text-[#4E7D40] sm:text-2xl [font-family:'Eagle_Lake',serif]">
                    Solicitud completada
                  </p>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      <div
        className="min-h-screen w-full"
        style={{ backgroundImage: `url(${fondoPergamino})`, backgroundSize: 'cover', backgroundPosition: 'center' }}
      >
        <div className="mx-auto max-w-md px-3 pb-20 pt-4">
          <div className="mb-3 flex flex-col items-center text-center">
            <img src={ecfLogo} alt="El Caldero Flameante" className="w-28 drop-shadow-lg sm:w-32" />
            <h1 className="mt-1 text-xl font-bold tracking-wide text-[#3e2723] [font-family:'Eagle_Lake',serif]">
              Mesa {tableCode ?? '--'}
            </h1>
          </div>

          <div className="mb-2 grid grid-cols-3 gap-1 rounded-lg border-2 border-[#8B5E3C] bg-[#e8d9c0]/80 p-1">
            <button
              onClick={() => setTab('requests')}
              className={`rounded-md py-1.5 text-[11px] font-bold [font-family:'Eagle_Lake',serif] ${tab === 'requests' ? 'bg-[#8B5E3C] text-[#f5ead8]' : 'text-[#3e2723]'}`}
            >
              Solicitudes
            </button>
            <button
              onClick={() => setTab('menu')}
              className={`rounded-md py-1.5 text-[11px] font-bold [font-family:'Eagle_Lake',serif] ${tab === 'menu' ? 'bg-[#8B5E3C] text-[#f5ead8]' : 'text-[#3e2723]'}`}
            >
              Menu
            </button>
            <button
              onClick={() => {
                setTab('order');
                loadActiveOrder(true).catch(() => {});
              }}
              className={`rounded-md py-1.5 text-[11px] font-bold [font-family:'Eagle_Lake',serif] ${tab === 'order' ? 'bg-[#8B5E3C] text-[#f5ead8]' : 'text-[#3e2723]'}`}
            >
              Pedido
            </button>
          </div>

          {tab === 'requests' ? (
            <section>
              <p className="mb-2 text-center text-xs text-[#6d4c3d] [font-family:'Eagle_Lake',serif]">
                Que necesitas?
              </p>
              <div className="grid grid-cols-2 gap-2">
                {requestOptions.map((option) => (
                  <RusticButton
                    key={option.id}
                    className="min-h-10"
                    onClick={() => handleOptionRequest(option.type, option.label)}
                  >
                    <div className="flex w-full flex-row items-center gap-2 px-1 text-[#3e2723]">
                      <img src={option.image} alt="" className="h-7 w-7 shrink-0 object-contain" />
                      <span className="min-w-0 flex-1 overflow-hidden text-left text-[11px] font-bold leading-tight [font-family:'Eagle_Lake',serif]">
                        {option.label}
                      </span>
                    </div>
                  </RusticButton>
                ))}
              </div>

              <div className="mt-4">
                <h2 className="text-center text-base font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif]">
                  Solicitud personalizada
                </h2>
                <textarea
                  value={customRequest}
                  onChange={(e) => setCustomRequest(e.target.value)}
                  placeholder="Escribe tu solicitud aqui..."
                  rows={4}
                  className="mt-2 box-border w-full resize-none rounded-lg border-2 border-[#8B5E3C] bg-[#f5f1ed] p-2.5 text-xs font-semibold text-[#3e2723] placeholder-[#a1887f] focus:outline-none focus:ring-2 focus:ring-[#bc955c]"
                />
                <button
                  onClick={handleCustomRequest}
                  disabled={isSubmitting || !customRequest.trim()}
                  className="mt-2 flex h-10 w-full items-center justify-center rounded-lg border-2 border-[#8B5E3C] bg-[#D2B48C] text-xs font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif] shadow-md transition-transform active:scale-95 disabled:opacity-50"
                >
                  {isSubmitting ? 'Enviando...' : 'Enviar solicitud'}
                </button>
              </div>
            </section>
          ) : tab === 'menu' ? (
            <section>
              {menuError && (
                <div className="mb-3 rounded-xl border-2 border-[#8B5E3C] bg-[#f5f1ed] p-3 text-center text-xs font-bold text-[#8B2E2E]">
                  {menuError}
                </div>
              )}
              {loadingMenu ? (
                <p className="py-8 text-center text-sm font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif]">Cargando menu...</p>
              ) : (
                <>
                  <div className="mb-2 flex gap-1.5 overflow-x-auto pb-1">
                    {categories.map(category => (
                      <button
                        key={category.id}
                        onClick={() => setSelectedCategoryId(category.id)}
                        className={`shrink-0 rounded-full border-2 border-[#8B5E3C] px-2.5 py-1 text-[11px] font-bold [font-family:'Eagle_Lake',serif] ${selectedCategoryId === category.id ? 'bg-[#8B5E3C] text-[#f5ead8]' : 'bg-[#e8d9c0] text-[#3e2723]'}`}
                      >
                        {category.name}
                      </button>
                    ))}
                  </div>

                  <div className="space-y-1.5">
                    {visibleItems.map(item => (
                      <button
                        key={item.id}
                        onClick={() => openItemDraft(item)}
                        disabled={!item.isAvailable}
                        className="w-full rounded-lg border-2 border-[#8B5E3C] bg-[#f5f1ed]/95 p-2.5 text-left shadow-md disabled:opacity-55"
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <h3 className="text-xs font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif]">{item.name}</h3>
                            {item.description && <p className="mt-0.5 text-[11px] text-[#6d4c3d]">{item.description}</p>}
                            {!item.isAvailable && <p className="mt-1 text-xs font-bold text-[#8B2E2E]">No disponible</p>}
                          </div>
                          <span className="shrink-0 rounded-full bg-[#8B5E3C] px-2 py-0.5 text-[11px] font-bold text-[#f5ead8]">
                            {money(item.price)}
                          </span>
                        </div>
                      </button>
                    ))}
                    {visibleItems.length === 0 && (
                      <p className="py-8 text-center text-sm font-bold text-[#6d4c3d]">No hay productos en esta categoria.</p>
                    )}
                  </div>
                </>
              )}

              {orderMessage && (
                <div className="mt-3 rounded-xl border-2 border-[#8B5E3C] bg-[#f5f1ed] p-3 text-center text-xs font-bold text-[#3e2723]">
                  {orderMessage}
                </div>
              )}
            </section>
          ) : (
            <section>
              {orderError && (
                <div className="mb-3 rounded-xl border-2 border-[#8B5E3C] bg-[#f5f1ed] p-3 text-center text-xs font-bold text-[#8B2E2E]">
                  {orderError}
                </div>
              )}

              {loadingOrder ? (
                <p className="py-8 text-center text-sm font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif]">Cargando pedido...</p>
              ) : activeOrder ? (
                <div className="rounded-lg border-2 border-[#8B5E3C] bg-[#f5f1ed]/95 p-2.5 shadow-md">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-[11px] font-bold text-[#6d4c3d]">Pedido #{activeOrder.number}</p>
                      <h2 className="mt-0.5 text-base font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif]">
                        {ORDER_STATUS_LABELS[activeOrder.status]?.label ?? activeOrder.status}
                      </h2>
                      <p className="mt-0.5 text-[11px] text-[#6d4c3d]">
                        {ORDER_STATUS_LABELS[activeOrder.status]?.detail ?? 'Estamos actualizando el estado de tu pedido.'}
                      </p>
                    </div>
                    <span className="shrink-0 rounded-full bg-[#8B5E3C] px-2 py-0.5 text-[11px] font-bold text-[#f5ead8]">
                      {money(activeOrder.total)}
                    </span>
                  </div>

                  <div className="mt-2 space-y-1.5">
                    {activeOrder.items.map(item => (
                      <div key={item.id} className="rounded-lg border-2 border-[#D2B48C] bg-white/70 p-2">
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <p className="text-xs font-bold text-[#3e2723]">
                              {item.quantity} x {item.itemName}
                            </p>
                            {item.modifierSelections.map(selection => (
                              <p key={`${selection.modifierOptionId}-${selection.optionName}`} className="text-[11px] text-[#6d4c3d]">
                                {selection.quantity} x {selection.optionName}
                                {selection.unitPriceDelta ? ` (${money(selection.unitPriceDelta)})` : ''}
                              </p>
                            ))}
                            {item.notes && <p className="mt-0.5 text-[11px] italic text-[#6d4c3d]">{item.notes}</p>}
                          </div>
                          <div className="text-right">
                            <p className="text-xs font-bold text-[#3e2723]">{money(item.totalPrice)}</p>
                            <p className="mt-0.5 text-[11px] text-[#6d4c3d]">{ORDER_STATUS_LABELS[item.status]?.label ?? item.status}</p>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>

                  <div className="mt-2 border-t-2 border-[#D2B48C] pt-2 text-[11px] font-bold text-[#3e2723]">
                    <div className="flex justify-between">
                      <span>Subtotal</span>
                      <span>{money(activeOrder.subtotal)}</span>
                    </div>
                    <div className="mt-1 flex justify-between">
                      <span>Impuestos</span>
                      <span>{money(activeOrder.taxAmount)}</span>
                    </div>
                    <div className="mt-1.5 flex justify-between text-sm">
                      <span>Total</span>
                      <span>{money(activeOrder.total)}</span>
                    </div>
                  </div>

                  <button
                    onClick={() => loadActiveOrder(true).catch(() => {})}
                    className="mt-2.5 h-9 w-full rounded-lg border-2 border-[#8B5E3C] bg-[#e8d9c0] text-xs font-bold text-[#3e2723]"
                  >
                    Actualizar estado
                  </button>
                </div>
              ) : (
                <div className="rounded-lg border-2 border-[#8B5E3C] bg-[#f5f1ed]/95 p-3 text-center shadow-md">
                  <h2 className="text-base font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif]">Sin pedido activo</h2>
                  <p className="mt-1.5 text-xs text-[#6d4c3d]">
                    Cuando envies un pedido desde el menu, lo podras ver aqui.
                  </p>
                  <button
                    onClick={() => setTab('menu')}
                    className="mt-2.5 h-9 w-full rounded-lg bg-[#8B5E3C] text-xs font-bold text-[#f5ead8]"
                  >
                    Ver menu
                  </button>
                </div>
              )}
            </section>
          )}
        </div>

        {cart.length > 0 && (
          <div className="fixed inset-x-0 bottom-0 z-30 border-t-2 border-[#8B5E3C] bg-[#2f1f18] px-3 py-1.5 text-[#f5ead8] shadow-2xl">
            <div className="mx-auto flex max-w-md items-center gap-3">
              <div className="min-w-0 flex-1">
                <p className="text-xs font-bold">{cartItemsCount} item(s) en carrito</p>
                <p className="text-sm font-bold">{money(cartTotal)}</p>
              </div>
              <button
                onClick={() => setItemDraft({
                  item: {
                    id: '__cart__',
                    menuCategoryId: '',
                    categoryName: '',
                    name: 'Carrito',
                    price: 0,
                    isAvailable: true,
                    hasModifiers: false,
                    modifierGroups: [],
                  },
                  quantity: 1,
                  notes: '',
                  selectedOptions: {},
                })}
                className="rounded-lg border-2 border-[#D2B48C] px-3 py-1.5 text-xs font-bold"
              >
                Ver
              </button>
              <button
                onClick={() => { submitDraftOrder().catch(() => {}); }}
                disabled={isSubmitting}
                className="rounded-lg bg-[#D2B48C] px-4 py-2 text-xs font-bold text-[#3e2723] disabled:opacity-60"
              >
                {isSubmitting ? 'Enviando...' : 'Enviar'}
              </button>
            </div>
          </div>
        )}
      </div>

      {pendingRequest && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
          <div
            className="w-full max-w-sm rounded-xl border-2 border-[#8B5E3C] shadow-2xl"
            style={{ backgroundImage: `url(${fondoPergamino})`, backgroundSize: 'cover' }}
          >
            <div className="px-4 pb-5 pt-3">
              <h3 className="text-center text-base font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif]">
                Confirmar solicitud?
              </h3>
              <p className="mt-1.5 text-center text-sm text-[#5d4037] [font-family:'Eagle_Lake',serif]">
                {pendingRequest.label}
              </p>
              {pendingRequest.customMessage && (
                <p className="mt-1 text-center text-sm italic text-[#7b5e47] [font-family:'Eagle_Lake',serif]">
                  &ldquo;{pendingRequest.customMessage}&rdquo;
                </p>
              )}
              <div className="mt-3 flex gap-2">
                <button
                  onClick={cancelRequest}
                  className="h-10 flex-1 rounded-lg border-2 border-[#8B5E3C] bg-[#e8d9c0] text-xs font-bold text-[#5d4037] [font-family:'Eagle_Lake',serif] active:scale-95"
                >
                  Cancelar
                </button>
                <button
                  onClick={() => { confirmRequest().catch(() => {}); }}
                  disabled={isSubmitting}
                  className="h-10 flex-1 rounded-lg border-2 border-[#5a3a1a] bg-[#8B5E3C] text-xs font-bold text-[#f5ead8] [font-family:'Eagle_Lake',serif] active:scale-95 disabled:opacity-50"
                >
                  {isSubmitting ? 'Enviando...' : 'Confirmar'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {itemDraft && itemDraft.item.id === '__cart__' && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/60 px-4 sm:items-center">
          <div className="max-h-[84vh] w-full max-w-md overflow-y-auto rounded-t-lg border-2 border-[#8B5E3C] bg-[#f5f1ed] p-3 shadow-2xl sm:rounded-lg">
            <h3 className="text-base font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif]">Carrito</h3>
            <div className="mt-2 space-y-1.5">
              {cart.map(line => (
                <div key={line.localId} className="rounded-lg border-2 border-[#D2B48C] bg-white/70 p-2">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-xs font-bold text-[#3e2723]">{line.itemName}</p>
                      {line.modifierLabels.map(label => <p key={label} className="text-[11px] text-[#6d4c3d]">{label}</p>)}
                      {line.notes && <p className="mt-0.5 text-[11px] italic text-[#6d4c3d]">{line.notes}</p>}
                    </div>
                    <p className="text-xs font-bold text-[#3e2723]">{money(line.unitPrice * line.quantity)}</p>
                  </div>
                  <div className="mt-2 flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <button className="h-7 w-7 rounded-full bg-[#e8d9c0] font-bold" onClick={() => updateCartQuantity(line.localId, -1)}>-</button>
                      <span className="w-7 text-center text-sm font-bold">{line.quantity}</span>
                      <button className="h-7 w-7 rounded-full bg-[#e8d9c0] font-bold" onClick={() => updateCartQuantity(line.localId, 1)}>+</button>
                    </div>
                    <button className="text-xs font-bold text-[#8B2E2E]" onClick={() => removeCartLine(line.localId)}>Quitar</button>
                  </div>
                </div>
              ))}
            </div>
            <div className="mt-3 flex gap-2">
              <button className="h-9 flex-1 rounded-lg border-2 border-[#8B5E3C] text-xs font-bold text-[#3e2723]" onClick={() => setItemDraft(null)}>Cerrar</button>
              <button className="h-9 flex-1 rounded-lg bg-[#8B5E3C] text-xs font-bold text-[#f5ead8]" onClick={() => { setItemDraft(null); submitDraftOrder().catch(() => {}); }}>
                Enviar pedido
              </button>
            </div>
          </div>
        </div>
      )}

      {itemDraft && itemDraft.item.id !== '__cart__' && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/60 px-4 sm:items-center">
          <div className="max-h-[86vh] w-full max-w-md overflow-y-auto rounded-t-lg border-2 border-[#8B5E3C] bg-[#f5f1ed] p-3 shadow-2xl sm:rounded-lg">
            <div className="flex items-start justify-between gap-3">
              <div>
                <h3 className="text-base font-bold text-[#3e2723] [font-family:'Eagle_Lake',serif]">{itemDraft.item.name}</h3>
                <p className="mt-0.5 text-xs font-bold text-[#8B5E3C]">{money(itemDraft.item.price)}</p>
              </div>
              <button className="text-xs font-bold text-[#6d4c3d]" onClick={() => setItemDraft(null)}>Cerrar</button>
            </div>

            <div className="mt-2.5 flex items-center justify-between rounded-lg bg-white/60 p-2">
              <span className="text-xs font-bold text-[#3e2723]">Cantidad</span>
              <div className="flex items-center gap-2">
                <button className="h-7 w-7 rounded-full bg-[#e8d9c0] font-bold" onClick={() => setItemDraft({ ...itemDraft, quantity: Math.max(1, itemDraft.quantity - 1) })}>-</button>
                <span className="w-7 text-center text-sm font-bold">{itemDraft.quantity}</span>
                <button className="h-7 w-7 rounded-full bg-[#e8d9c0] font-bold" onClick={() => setItemDraft({ ...itemDraft, quantity: itemDraft.quantity + 1 })}>+</button>
              </div>
            </div>

            {itemDraft.item.modifierGroups.map(group => {
              const selected = itemDraft.selectedOptions[group.id] ?? [];
              return (
                <div key={group.id} className="mt-2.5 rounded-lg border-2 border-[#D2B48C] bg-white/60 p-2">
                  <p className="text-xs font-bold text-[#3e2723]">{group.name}</p>
                  <p className="text-[11px] text-[#6d4c3d]">
                    {group.isRequired ? 'Requerido' : 'Opcional'} · elige {group.minSelections || (group.isRequired ? 1 : 0)} a {group.maxSelections}
                  </p>
                  <div className="mt-2 space-y-1.5">
                    {group.options.map(option => {
                      const active = selected.some(o => o.id === option.id);
                      return (
                        <button
                          key={option.id}
                          disabled={!option.isAvailable}
                          onClick={() => toggleModifierOption(group.id, option)}
                          className={`flex w-full items-center justify-between rounded-lg border-2 px-2 py-1.5 text-left text-[11px] ${active ? 'border-[#8B5E3C] bg-[#e8d9c0]' : 'border-[#D2B48C] bg-white/70'} disabled:opacity-50`}
                        >
                          <span className="font-bold text-[#3e2723]">{option.name}</span>
                          <span className="text-[#6d4c3d]">{option.isAvailable ? (option.priceDelta ? `+${money(option.priceDelta)}` : 'Incluido') : 'No disponible'}</span>
                        </button>
                      );
                    })}
                  </div>
                </div>
              );
            })}

            <textarea
              value={itemDraft.notes}
              onChange={(event) => setItemDraft({ ...itemDraft, notes: event.target.value })}
              placeholder="Observaciones: sin cebolla, salsa aparte..."
              rows={3}
              className="mt-2.5 box-border w-full resize-none rounded-lg border-2 border-[#8B5E3C] bg-white/80 p-2 text-[11px] text-[#3e2723] focus:outline-none"
            />

            {itemDraft.error && <p className="mt-2 text-xs font-bold text-[#8B2E2E]">{itemDraft.error}</p>}

            <button
              onClick={addDraftToCart}
              className="mt-2.5 h-10 w-full rounded-lg border-2 border-[#5a3a1a] bg-[#8B5E3C] text-xs font-bold text-[#f5ead8] [font-family:'Eagle_Lake',serif] active:scale-95"
            >
              Agregar al carrito
            </button>
          </div>
        </div>
      )}
    </>
  );
}
