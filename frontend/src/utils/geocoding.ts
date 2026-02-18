/**
 * Servicio de geocodificación usando Nominatim (OpenStreetMap)
 */

interface NominatimResponse {
  address?: {
    [key: string]: string;
  };
  display_name?: string;
}

/**
 * Obtiene la dirección a partir de coordenadas (reverse geocoding)
 * @param latitude Latitud
 * @param longitude Longitud
 * @returns Dirección formateada
 */
export const getAddressFromCoordinates = async (
  latitude: number,
  longitude: number
): Promise<string> => {
  try {
    const response = await fetch(
      `https://nominatim.openstreetmap.org/reverse?format=json&lat=${latitude}&lon=${longitude}`,
      {
        headers: {
          'Accept': 'application/json',
        },
      }
    );

    if (!response.ok) {
      throw new Error('Error al obtener la dirección');
    }

    const data: NominatimResponse = await response.json();
    
    // Construir dirección corta con los campos más importantes
    if (data.address) {
      const addr = data.address;
      const parts: string[] = [];
      
      // Agregar calle con número
      let streetAddress = '';
      if (addr.road || addr.street) {
        streetAddress = addr.road || addr.street;
        // Agregar número de casa si existe
        if (addr.house_number) {
          streetAddress += ` ${addr.house_number}`;
        }
        parts.push(streetAddress);
      }
      
      // Agregar ciudad o pueblo
      if (addr.city || addr.town || addr.village) {
        parts.push(addr.city || addr.town || addr.village);
      }
      
      // Si tenemos al menos calle y ciudad, usar eso
      if (parts.length > 0) {
        return parts.join(', ');
      }
    }
    
    // Si no se puede construir una dirección corta, usar display_name
    return data.display_name || 'Dirección no disponible';
  } catch (error) {
    console.error('Error en reverse geocoding:', error);
    return 'Error al obtener la dirección';
  }
};

/**
 * Obtiene coordenadas a partir de una dirección (geocoding)
 * @param address Dirección
 * @returns Objeto con latitud y longitud
 */
export const getCoordinatesFromAddress = async (
  address: string
): Promise<{ latitude: number; longitude: number } | null> => {
  try {
    const response = await fetch(
      `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(address)}&format=json&limit=1`,
      {
        headers: {
          'Accept': 'application/json',
        },
      }
    );

    if (!response.ok) {
      throw new Error('Error al obtener coordenadas');
    }

    const data = await response.json();

    if (data.length === 0) {
      return null;
    }

    return {
      latitude: parseFloat(data[0].lat),
      longitude: parseFloat(data[0].lon),
    };
  } catch (error) {
    console.error('Error en geocoding:', error);
    return null;
  }
};
