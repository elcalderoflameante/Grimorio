import { theme, type ThemeConfig } from 'antd';

export const GRIMORIO_COLORS = {
  bg0: '#0e0d0a',
  bg1: '#1b1814',
  bg2: '#2a2520',
  surface: '#15130f',
  surfaceElev: '#1f1b15',
  border: 'rgba(180,140,80,0.22)',
  borderStrong: 'rgba(180,140,80,0.55)',
  gold: '#b88a3a',
  goldBright: '#d9a655',
  ember: '#ff6a1a',
  emberDeep: '#a02e08',
  cream: '#e8dcc0',
  parchment: '#f4ead6',
  parchmentSoft: '#fbf7ed',
  ink: '#2b2118',
  inkMuted: 'rgba(43,33,24,0.62)',
  danger: '#e07050',
};

export function hexA(hex: string, alpha: number) {
  const h = hex.replace('#', '');
  const value = h.length === 3 ? h.split('').map((c) => c + c).join('') : h;
  const r = parseInt(value.slice(0, 2), 16);
  const g = parseInt(value.slice(2, 4), 16);
  const b = parseInt(value.slice(4, 6), 16);
  return `rgba(${r},${g},${b},${alpha})`;
}

export const grimorioAppTheme: ThemeConfig = {
  algorithm: theme.defaultAlgorithm,
  token: {
    colorPrimary: GRIMORIO_COLORS.gold,
    colorInfo: GRIMORIO_COLORS.gold,
    colorSuccess: '#4f7d45',
    colorWarning: GRIMORIO_COLORS.ember,
    colorError: GRIMORIO_COLORS.danger,
    colorBgBase: GRIMORIO_COLORS.parchmentSoft,
    colorBgLayout: '#f3eadc',
    colorBgContainer: '#fffaf0',
    colorBgElevated: '#fffdf7',
    colorBorder: 'rgba(88, 62, 35, 0.16)',
    colorBorderSecondary: 'rgba(88, 62, 35, 0.1)',
    colorText: GRIMORIO_COLORS.ink,
    colorTextSecondary: GRIMORIO_COLORS.inkMuted,
    colorLink: '#9a6b25',
    colorLinkHover: GRIMORIO_COLORS.goldBright,
    borderRadius: 8,
    borderRadiusLG: 10,
    controlHeight: 36,
    controlHeightLG: 42,
    fontFamily: 'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
    boxShadow: '0 10px 28px rgba(64, 42, 12, 0.1)',
    boxShadowSecondary: '0 8px 20px rgba(64, 42, 12, 0.08)',
  },
  components: {
    Alert: {
      borderRadiusLG: 8,
    },
    Button: {
      primaryShadow: `0 8px 18px ${hexA(GRIMORIO_COLORS.ember, 0.18)}`,
    },
    Card: {
      borderRadiusLG: 10,
      colorBorderSecondary: 'rgba(88, 62, 35, 0.12)',
    },
    Layout: {
      bodyBg: '#f3eadc',
      headerBg: '#fffaf0',
      siderBg: GRIMORIO_COLORS.surface,
    },
    Menu: {
      darkItemBg: GRIMORIO_COLORS.surface,
      darkSubMenuItemBg: '#100e0b',
      darkItemSelectedBg: '#8b6429',
      darkItemColor: 'rgba(232,220,192,0.76)',
      darkItemHoverColor: GRIMORIO_COLORS.cream,
      darkItemSelectedColor: '#fff8e8',
      itemSelectedBg: 'rgba(184, 138, 58, 0.14)',
      itemSelectedColor: '#8a5f1f',
      itemHoverColor: '#8a5f1f',
    },
    Modal: {
      contentBg: '#fffaf0',
      headerBg: '#fffaf0',
    },
    Table: {
      headerBg: '#f5ead8',
      headerColor: GRIMORIO_COLORS.ink,
      rowHoverBg: '#fff4df',
      borderColor: 'rgba(88, 62, 35, 0.12)',
    },
    Tabs: {
      itemSelectedColor: '#8a5f1f',
      inkBarColor: GRIMORIO_COLORS.gold,
    },
  },
};

export const grimorioLoginTheme: ThemeConfig = {
  algorithm: theme.darkAlgorithm,
  token: {
    colorPrimary: GRIMORIO_COLORS.gold,
    colorInfo: GRIMORIO_COLORS.gold,
    colorBgBase: GRIMORIO_COLORS.surface,
    colorBgContainer: 'rgba(255,255,255,0.04)',
    colorBgElevated: GRIMORIO_COLORS.surfaceElev,
    colorBorder: GRIMORIO_COLORS.border,
    colorError: GRIMORIO_COLORS.danger,
    colorIcon: 'rgba(232,220,192,0.6)',
    colorIconHover: GRIMORIO_COLORS.gold,
    colorText: GRIMORIO_COLORS.cream,
    colorTextBase: GRIMORIO_COLORS.cream,
    colorTextPlaceholder: 'rgba(232,220,192,0.38)',
    colorTextSecondary: 'rgba(232,220,192,0.6)',
    borderRadius: 8,
    borderRadiusLG: 10,
    controlHeight: 44,
    controlHeightLG: 48,
    fontFamily: 'Inter, ui-sans-serif, system-ui, -apple-system, sans-serif',
    fontSize: 14,
    lineWidth: 1,
  },
  components: {
    Button: {
      primaryShadow: `0 8px 24px ${hexA(GRIMORIO_COLORS.ember, 0.35)}`,
    },
    Checkbox: {
      colorPrimary: GRIMORIO_COLORS.gold,
    },
    Form: {
      labelColor: 'rgba(232,220,192,0.6)',
    },
    Input: {
      activeBorderColor: GRIMORIO_COLORS.gold,
      activeShadow: `0 0 0 2px ${hexA(GRIMORIO_COLORS.gold, 0.18)}`,
      hoverBorderColor: GRIMORIO_COLORS.borderStrong,
    },
  },
};
