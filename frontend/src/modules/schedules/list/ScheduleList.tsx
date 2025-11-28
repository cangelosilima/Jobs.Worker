import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Button,
  Alert,
  CircularProgress,
  Chip,
} from '@mui/material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import { Add as AddIcon, Refresh as RefreshIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { jobsApi } from '@/api/jobs.api';
import { RecurrenceType, JobDefinitionResponse } from '@/api/types';
import { useAuthStore } from '@/state/auth.store';
import dayjs from 'dayjs';

const getRecurrenceTypeLabel = (type: RecurrenceType): string => {
  switch (type) {
    case RecurrenceType.Daily:
      return 'Daily';
    case RecurrenceType.Weekly:
      return 'Weekly';
    case RecurrenceType.Monthly:
      return 'Monthly';
    case RecurrenceType.MonthlyBusinessDay:
      return 'Monthly (Business Day)';
    case RecurrenceType.Cron:
      return 'Cron';
    case RecurrenceType.OneTime:
      return 'One-time';
    case RecurrenceType.Conditional:
      return 'Conditional';
    default:
      return 'Unknown';
  }
};

const ScheduleList = () => {
  const navigate = useNavigate();
  const { canEdit } = useAuthStore();

  const {
    data: jobs,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['allJobs'],
    queryFn: jobsApi.getAll,
  });

  // Flatten schedules with job info
  const schedules = jobs?.flatMap((job) =>
    job.schedules.map((schedule) => ({
      ...schedule,
      jobName: job.name,
      jobStatus: job.status,
    }))
  ) || [];

  const columns: GridColDef[] = [
    {
      field: 'jobName',
      headerName: 'Job Name',
      flex: 1,
      minWidth: 200,
      renderCell: (params) => (
        <Button
          variant="text"
          onClick={() => navigate(`/jobs/${params.row.jobId}`)}
          sx={{ textTransform: 'none', justifyContent: 'flex-start' }}
        >
          {params.value}
        </Button>
      ),
    },
    {
      field: 'recurrenceType',
      headerName: 'Type',
      width: 180,
      valueFormatter: (value) => getRecurrenceTypeLabel(value),
    },
    {
      field: 'cronExpression',
      headerName: 'Cron Expression',
      width: 150,
      renderCell: (params) =>
        params.value ? (
          <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
            {params.value}
          </Typography>
        ) : (
          '-'
        ),
    },
    {
      field: 'nextExecutionTime',
      headerName: 'Next Run',
      width: 200,
      valueFormatter: (value) => (value ? dayjs(value).format('MMM DD, YYYY HH:mm') : '-'),
    },
    {
      field: 'lastExecutionTime',
      headerName: 'Last Run',
      width: 200,
      valueFormatter: (value) => (value ? dayjs(value).format('MMM DD, YYYY HH:mm') : 'Never'),
    },
    {
      field: 'isActive',
      headerName: 'Status',
      width: 120,
      renderCell: (params) => (
        <Chip
          label={params.value ? 'Active' : 'Inactive'}
          color={params.value ? 'success' : 'default'}
          size="small"
        />
      ),
    },
    {
      field: 'startDate',
      headerName: 'Start Date',
      width: 150,
      valueFormatter: (value) => dayjs(value).format('MMM DD, YYYY'),
    },
    {
      field: 'endDate',
      headerName: 'End Date',
      width: 150,
      valueFormatter: (value) => (value ? dayjs(value).format('MMM DD, YYYY') : 'No end'),
    },
  ];

  if (error) {
    return (
      <Alert severity="error">
        Error loading schedules: {error instanceof Error ? error.message : 'Unknown error'}
      </Alert>
    );
  }

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4">Job Schedules</Typography>
        <Box>
          {canEdit() && (
            <Button
              startIcon={<AddIcon />}
              onClick={() => navigate('/schedules/create')}
              variant="contained"
              sx={{ mr: 1 }}
            >
              Create Schedule
            </Button>
          )}
          <Button startIcon={<RefreshIcon />} onClick={() => refetch()} variant="outlined">
            Refresh
          </Button>
        </Box>
      </Box>

      <Card>
        <CardContent>
          {isLoading ? (
            <Box display="flex" justifyContent="center" p={4}>
              <CircularProgress />
            </Box>
          ) : schedules.length === 0 ? (
            <Box textAlign="center" p={4}>
              <Typography color="text.secondary">No schedules found</Typography>
            </Box>
          ) : (
            <>
              <Box mb={2}>
                <Typography variant="body2" color="text.secondary">
                  {schedules.length} schedule{schedules.length !== 1 ? 's' : ''} found
                </Typography>
              </Box>
              <DataGrid
                rows={schedules}
                columns={columns}
                autoHeight
                disableRowSelectionOnClick
                pageSizeOptions={[10, 25, 50]}
                initialState={{
                  pagination: { paginationModel: { pageSize: 25 } },
                }}
              />
            </>
          )}
        </CardContent>
      </Card>
    </Box>
  );
};

export default ScheduleList;
