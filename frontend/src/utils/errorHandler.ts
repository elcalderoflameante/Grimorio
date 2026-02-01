import type { AxiosError } from 'axios';

interface ErrorResponse {
  message?: string;
  error?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

const isAxiosError = (error: unknown): error is AxiosError<ErrorResponse> =>
  typeof error === 'object' && error !== null && (error as AxiosError).isAxiosError === true;

/**
 * Obtiene un mensaje de error descriptivo basado en el código HTTP y el contenido
 */
export const getErrorMessage = (error: AxiosError<ErrorResponse>): string => {
  // Si ya tiene un mensaje específico del servidor
  if (error.response?.data?.message) {
    return error.response.data.message;
  }

  // Si hay un objeto de errores (validación)
  if (error.response?.data?.errors) {
    const errors = error.response.data.errors as Record<string, string[]>;
    const firstError = Object.values(errors)?.[0]?.[0];
    if (firstError) {
      return firstError;
    }
  }

  // Basarse en el código de estado HTTP
  const status = error.response?.status;
  const url = error.config?.url || '';
  const pathAfterApi = url.includes('/api/') ? url.split('/api/')[1] : url;

  switch (status) {
    case 400:
      return 'Solicitud inválida. Verifique los datos ingresados.';
    case 401:
      return 'No autorizado. Por favor inicie sesión nuevamente.';
    case 403:
      return 'No tiene permiso para realizar esta acción.';
    case 404:
      // Patrones específicos para identificar qué recurso no existe
      if (/^users\/[^/]+$/.test(pathAfterApi)) return 'El usuario no existe.';
      if (/^employees\/[^/]+$/.test(pathAfterApi)) return 'El empleado no existe.';
      if (/^roles\/[^/]+$/.test(pathAfterApi)) return 'El rol no existe.';
      if (/^positions\/[^/]+$/.test(pathAfterApi)) return 'La posición no existe.';
      if (/^scheduling\/work-areas\/[^/]+$/.test(pathAfterApi)) return 'El área de trabajo no existe.';
      if (/^scheduling\/work-roles\/[^/]+$/.test(pathAfterApi)) return 'El rol de trabajo no existe.';
      if (/^permissions\/[^/]+$/.test(pathAfterApi)) return 'El permiso no existe.';
      if (/^scheduling\/employees\/[^/]+\/availability\/[^/]+$/.test(pathAfterApi)) return 'La fecha de indisponibilidad no existe.';
      if (/^scheduling\/shifts\/[^/]+$/.test(pathAfterApi)) return 'El turno no existe.';
      
      // Si es un 404 pero no es un recurso específico, es probable que sea un endpoint no encontrado
      if (url.includes('/api/')) {
        return 'El servidor no pudo procesar la solicitud. Verifique su conexión e intente nuevamente.';
      }
      return 'El recurso solicitado no existe.';
    case 409:
      return 'Conflicto: Este registro ya existe o hay un conflicto con los datos.';
    case 422:
      return 'Los datos ingresados no son válidos. Verifique la información.';
    case 500:
      return 'Error interno del servidor. Por favor intente más tarde.';
    case 502:
      return 'Error de conexión. El servidor no está disponible.';
    case 503:
      return 'El servidor está en mantenimiento. Por favor intente más tarde.';
    case 504:
      return 'Tiempo de espera agotado. Por favor intente más tarde.';
    default:
      if (error?.code === 'ECONNABORTED') {
        return 'La solicitud tardó demasiado tiempo. Intente de nuevo.';
      }
      if (error?.code === 'ERR_NETWORK') {
        return 'Error de conexión de red. Verifique su conexión a internet.';
      }
      return error?.message || 'Ocurrió un error. Por favor intente más tarde.';
  }
};

/**
 * Formatea un error para mostrar en la UI
 */
export const formatError = (error: unknown): string => {
  if (!error) {
    return 'Error desconocido';
  }

  // Si es un string
  if (typeof error === 'string') {
    return error;
  }

  // Si tiene estructura de AxiosError
  if (isAxiosError(error)) {
    return getErrorMessage(error);
  }

  // Si tiene un mensaje
  if (error instanceof Error) {
    return error.message;
  }

  return 'Error desconocido';
};

/**
 * Extrae información detallada del error para debugging
 */
export const getDetailedError = (error: unknown) => {
  if (isAxiosError(error)) {
    return {
      status: error.response?.status,
      statusText: error.response?.statusText,
      message: error.response?.data?.message || error.message,
      detail: error.response?.data?.detail || error.response?.data?.error,
      url: error.config?.url,
      method: error.config?.method,
      fullUrl: `${error.config?.baseURL}${error.config?.url}`,
    };
  }

  const message = error instanceof Error ? error.message : undefined;

  return {
    status: undefined,
    statusText: undefined,
    message,
    detail: undefined,
    url: undefined,
    method: undefined,
    fullUrl: undefined,
  };
};
