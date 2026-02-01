import { Card, Typography } from 'antd';
import { useAuth } from '../../context/AuthContext';
import logo from '../../assets/ECF-Logo.png';

const { Title, Paragraph } = Typography;

export const Welcome = () => {
  const { user } = useAuth();

  return (
    <div style={{ 
      display: 'flex', 
      justifyContent: 'center', 
      alignItems: 'center', 
      minHeight: '60vh' 
    }}>
      <Card 
        style={{ 
          maxWidth: 600, 
          width: '100%',
          textAlign: 'center',
          boxShadow: '0 4px 12px rgba(0,0,0,0.1)'
        }}
      >
        <img 
          src={logo} 
          alt="Logo ECF" 
          style={{ 
            height: 100,
            width: 'auto',
            objectFit: 'contain',
            marginBottom: 24
          }} 
        />
        <Title level={2}>
          ¡Bienvenido{user?.firstName ? `, ${user.firstName}` : ''}!
        </Title>
        <Paragraph style={{ fontSize: 16, color: '#666' }}>
          Sistema de Gestión Grimorio
        </Paragraph>
        <Paragraph>
          Utiliza el menú lateral para navegar entre las diferentes secciones del sistema.
        </Paragraph>
      </Card>
    </div>
  );
};

export default Welcome;
