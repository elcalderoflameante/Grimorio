import React from 'react';
import ReactDOM from 'react-dom/client';
import { App, ConfigProvider } from 'antd';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';
import SalesProfitability from '../../src/components/Billing/SalesProfitability';

dayjs.extend(utc);
dayjs.extend(timezone);

ReactDOM.createRoot(document.getElementById('root')!).render(
  <ConfigProvider><App><SalesProfitability /></App></ConfigProvider>,
);
