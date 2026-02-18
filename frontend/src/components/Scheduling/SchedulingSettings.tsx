import React, { useState } from 'react';
import { Tabs } from 'antd';
import { SettingOutlined, AppstoreOutlined, TeamOutlined, ClockCircleOutlined, CalendarOutlined } from '@ant-design/icons';
import {
  ScheduleConfigurationForm,
  WorkAreaList,
  WorkRoleList,
  ShiftTemplateList,
  SpecialDateListWithTemplates,
} from './index';

interface SchedulingSettingsProps {
  branchId: string;
}

export const SchedulingSettings: React.FC<SchedulingSettingsProps> = ({ branchId }) => {
  const [activeTab, setActiveTab] = useState('general');

  const tabs = [
    {
      key: 'general',
      label: (
        <span>
          <SettingOutlined /> Configuración General
        </span>
      ),
      children: <ScheduleConfigurationForm branchId={branchId} />,
    },
    {
      key: 'work-areas',
      label: (
        <span>
          <AppstoreOutlined /> Áreas de Trabajo
        </span>
      ),
      children: <WorkAreaList branchId={branchId} />,
    },
    {
      key: 'work-roles',
      label: (
        <span>
          <TeamOutlined /> Roles de Trabajo
        </span>
      ),
      children: <WorkRoleList branchId={branchId} />,
    },
    {
      key: 'shift-templates',
      label: (
        <span>
          <ClockCircleOutlined /> Plantillas de Turnos
        </span>
      ),
      children: <ShiftTemplateList branchId={branchId} />,
    },
    {
      key: 'special-dates',
      label: (
        <span>
          <CalendarOutlined /> Días Especiales
        </span>
      ),
      children: <SpecialDateListWithTemplates branchId={branchId} />,
    },
  ];

  return (
    <Tabs
      activeKey={activeTab}
      onChange={setActiveTab}
      items={tabs}
      type="card"
    />
  );
};

export default SchedulingSettings;
