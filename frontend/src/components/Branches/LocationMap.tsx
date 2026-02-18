import { useState, useEffect } from 'react';
import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet';
import type { LatLngTuple, LeafletMouseEvent } from 'leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { getAddressFromCoordinates } from '../../utils/geocoding';

// Solucionar problema de iconos en React Leaflet
const iconPrototype = L.Icon.Default.prototype as { _getIconUrl?: unknown };
delete iconPrototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
});

interface LocationMapProps {
  latitude?: number;
  longitude?: number;
  onLocationChange: (lat: number, lng: number, address?: string) => void;
  height?: string;
}

interface LocationMarkerProps {
  latitude?: number;
  longitude?: number;
  onLocationChange: (lat: number, lng: number, address?: string) => void;
}

const LocationMarker = ({ latitude, longitude, onLocationChange }: LocationMarkerProps) => {
  const [position, setPosition] = useState<LatLngTuple | null>(
    latitude && longitude ? [latitude, longitude] : null
  );

  // Actualizar posición cuando los props cambian (ej: al cargar datos guardados)
  useEffect(() => {
    if (latitude && longitude) {
      setPosition([latitude, longitude]);
    }
  }, [latitude, longitude]);

  useMapEvents({
    click(e: LeafletMouseEvent) {
      const { lat, lng } = e.latlng;
      setPosition([lat, lng]);
      
      // Obtener dirección de las coordenadas
      getAddressFromCoordinates(lat, lng).then((address) => {
        onLocationChange(lat, lng, address);
      });
    },
  });

  return position === null ? null : (
    <Marker position={position} />
  );
};

export const LocationMap = ({
  latitude,
  longitude,
  onLocationChange,
  height = '400px',
}: LocationMapProps) => {
  const center: LatLngTuple = [
    latitude && longitude ? latitude : -0.2298,
    longitude && latitude ? longitude : -78.5249,
  ];

  return (
    <div style={{ height, borderRadius: '4px', overflow: 'hidden' }}>
      <MapContainer
        center={center as LatLngTuple}
        zoom={13}
        style={{ height: '100%', width: '100%' }}
      >
        <TileLayer
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          noWrap={false}
        />
        <LocationMarker
          latitude={latitude}
          longitude={longitude}
          onLocationChange={onLocationChange}
        />
      </MapContainer>
    </div>
  );
};
