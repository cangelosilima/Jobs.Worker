import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { useTheme } from '@mui/material/styles';
import { ExecutionTrendResponse } from '@/api/types';
import dayjs from 'dayjs';

interface ExecutionTrendsChartProps {
  data: ExecutionTrendResponse[];
}

export const ExecutionTrendsChart: React.FC<ExecutionTrendsChartProps> = ({ data }) => {
  const theme = useTheme();

  const chartData = data.map((item) => ({
    date: dayjs(item.date).format('MMM DD'),
    Succeeded: item.succeeded,
    Failed: item.failed,
    'Timed Out': item.timedOut,
    Cancelled: item.cancelled,
  }));

  return (
    <ResponsiveContainer width="100%" height={300}>
      <LineChart data={chartData}>
        <CartesianGrid strokeDasharray="3 3" stroke={theme.palette.divider} />
        <XAxis
          dataKey="date"
          stroke={theme.palette.text.secondary}
          style={{ fontSize: '0.875rem' }}
        />
        <YAxis
          stroke={theme.palette.text.secondary}
          style={{ fontSize: '0.875rem' }}
        />
        <Tooltip
          contentStyle={{
            backgroundColor: theme.palette.background.paper,
            border: `1px solid ${theme.palette.divider}`,
            borderRadius: theme.shape.borderRadius,
          }}
        />
        <Legend />
        <Line
          type="monotone"
          dataKey="Succeeded"
          stroke={theme.palette.success.main}
          strokeWidth={2}
          dot={{ r: 4 }}
        />
        <Line
          type="monotone"
          dataKey="Failed"
          stroke={theme.palette.error.main}
          strokeWidth={2}
          dot={{ r: 4 }}
        />
        <Line
          type="monotone"
          dataKey="Timed Out"
          stroke={theme.palette.warning.main}
          strokeWidth={2}
          dot={{ r: 4 }}
        />
        <Line
          type="monotone"
          dataKey="Cancelled"
          stroke={theme.palette.text.secondary}
          strokeWidth={2}
          dot={{ r: 4 }}
        />
      </LineChart>
    </ResponsiveContainer>
  );
};
