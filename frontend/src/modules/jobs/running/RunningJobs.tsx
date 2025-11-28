import { useEffect, useState } from 'react';
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
  LinearProgress,
} from '@mui/material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import { Refresh as RefreshIcon, Stop as StopIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { executionsApi } from '@/api/jobs.api';
import { signalRService, JobExecutionUpdate } from '@/services/signalr';
import { ExecutionStatus, JobExecutionResponse } from '@/api/types';
import { useAuthStore } from '@/state/auth.store';
import dayjs from 'dayjs';
import duration from 'dayjs/plugin/duration';

dayjs.extend(duration);

const getStatusColor = (status: ExecutionStatus): 'default' | 'primary' | 'success' | 'error' | 'warning' => {
  switch (status) {
    case ExecutionStatus.Running:
      return 'primary';
    case ExecutionStatus.Succeeded:
      return 'success';
    case ExecutionStatus.Failed:
    case ExecutionStatus.TimedOut:
      return 'error';
    case ExecutionStatus.Cancelled:
    case ExecutionStatus.Skipped:
      return 'warning';
    default:
      return 'default';
  }
};

const getStatusLabel = (status: ExecutionStatus): string => {
  switch (status) {
    case ExecutionStatus.Queued:
      return 'Queued';
    case ExecutionStatus.Running:
      return 'Running';
    case ExecutionStatus.Succeeded:
      return 'Succeeded';
    case ExecutionStatus.Failed:
      return 'Failed';
    case ExecutionStatus.TimedOut:
      return 'Timed Out';
    case ExecutionStatus.Cancelled:
      return 'Cancelled';
    case ExecutionStatus.Skipped:
      return 'Skipped';
    case ExecutionStatus.Retrying:
      return 'Retrying';
    default:
      return 'Unknown';
  }
};

const RunningJobs = () => {
  const navigate = useNavigate();
  const { canTrigger } = useAuthStore();
  const [localExecutions, setLocalExecutions] = useState<JobExecutionResponse[]>([]);

  const {
    data: runningJobs,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['runningExecutions'],
    queryFn: executionsApi.getRunning,
    refetchInterval: 5000, // Poll every 5 seconds as fallback
  });

  // Initialize local state when data loads
  useEffect(() => {
    if (runningJobs) {
      setLocalExecutions(runningJobs);
    }
  }, [runningJobs]);

  // Subscribe to SignalR real-time updates
  useEffect(() => {
    const handleExecutionUpdate = (update: JobExecutionUpdate) => {
      setLocalExecutions((prev) => {
        const index = prev.findIndex((e) => e.id === update.executionId);
        if (index >= 0) {
          // Update existing execution
          const updated = [...prev];
          updated[index] = {
            ...updated[index],
            status: update.status as ExecutionStatus,
            startTime: update.startTime || updated[index].startTime,
            endTime: update.endTime || updated[index].endTime,
            output: update.output || updated[index].output,
            errorMessage: update.errorMessage || updated[index].errorMessage,
          };
          return updated;
        }
        // Refresh if we don't have this execution
        refetch();
        return prev;
      });
    };

    const handleJobStarted = (update: JobExecutionUpdate) => {
      refetch();
    };

    const handleJobCompleted = (update: JobExecutionUpdate) => {
      setLocalExecutions((prev) => prev.filter((e) => e.id !== update.executionId));
    };

    signalRService.on('JobExecutionUpdated', handleExecutionUpdate);
    signalRService.on('JobStarted', handleJobStarted);
    signalRService.on('JobCompleted', handleJobCompleted);
    signalRService.on('JobFailed', handleJobCompleted);

    return () => {
      signalRService.off('JobExecutionUpdated', handleExecutionUpdate);
      signalRService.off('JobStarted', handleJobStarted);
      signalRService.off('JobCompleted', handleJobCompleted);
      signalRService.off('JobFailed', handleJobCompleted);
    };
  }, [refetch]);

  const handleCancelExecution = async (executionId: string) => {
    try {
      await executionsApi.cancel({
        executionId,
        cancelledBy: 'current-user', // TODO: Get from auth
        reason: 'Cancelled by user',
      });
      refetch();
    } catch (error) {
      console.error('Failed to cancel execution:', error);
    }
  };

  const columns: GridColDef[] = [
    {
      field: 'jobName',
      headerName: 'Job Name',
      flex: 1,
      minWidth: 200,
      renderCell: (params: GridRenderCellParams<JobExecutionResponse>) => (
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
      field: 'status',
      headerName: 'Status',
      width: 130,
      renderCell: (params: GridRenderCellParams<JobExecutionResponse>) => (
        <Chip
          label={getStatusLabel(params.value)}
          color={getStatusColor(params.value)}
          size="small"
        />
      ),
    },
    {
      field: 'startTime',
      headerName: 'Started',
      width: 180,
      valueFormatter: (value) => (value ? dayjs(value).format('MMM DD, HH:mm:ss') : '-'),
    },
    {
      field: 'durationSeconds',
      headerName: 'Duration',
      width: 120,
      valueGetter: (value, row) => {
        if (!row.startTime) return null;
        const start = dayjs(row.startTime);
        const end = row.endTime ? dayjs(row.endTime) : dayjs();
        return end.diff(start, 'second');
      },
      valueFormatter: (value) => {
        if (!value) return '-';
        const dur = dayjs.duration(value, 'seconds');
        if (dur.asHours() >= 1) {
          return dur.format('H[h] m[m] s[s]');
        } else if (dur.asMinutes() >= 1) {
          return dur.format('m[m] s[s]');
        }
        return dur.format('s[s]');
      },
    },
    {
      field: 'retryCount',
      headerName: 'Retries',
      width: 100,
      align: 'center',
      headerAlign: 'center',
    },
    {
      field: 'correlationId',
      headerName: 'Correlation ID',
      width: 150,
      renderCell: (params: GridRenderCellParams<JobExecutionResponse>) => (
        <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>
          {params.value.substring(0, 8)}...
        </Typography>
      ),
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 120,
      sortable: false,
      renderCell: (params: GridRenderCellParams<JobExecutionResponse>) => (
        <Button
          size="small"
          startIcon={<StopIcon />}
          color="error"
          onClick={() => handleCancelExecution(params.row.id)}
          disabled={
            !canTrigger() ||
            params.row.status !== ExecutionStatus.Running
          }
        >
          Cancel
        </Button>
      ),
    },
  ];

  if (error) {
    return (
      <Alert severity="error">
        Error loading running jobs: {error instanceof Error ? error.message : 'Unknown error'}
      </Alert>
    );
  }

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4">Running Jobs</Typography>
        <Button startIcon={<RefreshIcon />} onClick={() => refetch()} variant="outlined">
          Refresh
        </Button>
      </Box>

      <Card>
        <CardContent>
          {isLoading ? (
            <Box display="flex" justifyContent="center" p={4}>
              <CircularProgress />
            </Box>
          ) : localExecutions.length === 0 ? (
            <Box textAlign="center" p={4}>
              <Typography color="text.secondary">No jobs currently running</Typography>
            </Box>
          ) : (
            <>
              <Box mb={2}>
                <Typography variant="body2" color="text.secondary">
                  {localExecutions.length} job{localExecutions.length !== 1 ? 's' : ''} running • Real-time updates enabled
                </Typography>
                {signalRService.isConnected() && (
                  <Chip
                    label="Live"
                    color="success"
                    size="small"
                    sx={{ ml: 1 }}
                  />
                )}
              </Box>
              <DataGrid
                rows={localExecutions}
                columns={columns}
                autoHeight
                disableRowSelectionOnClick
                pageSizeOptions={[10, 25, 50]}
                initialState={{
                  pagination: { paginationModel: { pageSize: 10 } },
                }}
                slots={{
                  loadingOverlay: LinearProgress,
                }}
              />
            </>
          )}
        </CardContent>
      </Card>
    </Box>
  );
};

export default RunningJobs;
