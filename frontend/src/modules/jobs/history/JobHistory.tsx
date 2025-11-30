import { useState } from 'react';
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
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  MenuItem,
  Grid,
  IconButton,
  Collapse,
} from '@mui/material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import {
  Refresh as RefreshIcon,
  FilterList as FilterListIcon,
  Visibility as VisibilityIcon,
  ExpandLess as ExpandLessIcon,
} from '@mui/icons-material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { useNavigate } from 'react-router-dom';
import { executionsApi } from '@/api/jobs.api';
import { ExecutionStatus, JobExecutionResponse, JobExecutionFilter } from '@/api/types';
import dayjs, { Dayjs } from 'dayjs';
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

const JobHistory = () => {
  const navigate = useNavigate();
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(25);
  const [filterOpen, setFilterOpen] = useState(false);
  const [selectedExecution, setSelectedExecution] = useState<JobExecutionResponse | null>(null);
  const [detailsOpen, setDetailsOpen] = useState(false);

  const [filter, setFilter] = useState<JobExecutionFilter>({
    pageNumber: 1,
    pageSize: 25,
  });

  const [tempFilter, setTempFilter] = useState(filter);

  const {
    data: executionsData,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['jobExecutions', filter],
    queryFn: () => executionsApi.getAll(filter),
  });

  const handleApplyFilter = () => {
    setFilter({ ...tempFilter, pageNumber: 1 });
    setPage(0);
    setFilterOpen(false);
  };

  const handleClearFilter = () => {
    const clearedFilter: JobExecutionFilter = {
      pageNumber: 1,
      pageSize: pageSize,
    };
    setTempFilter(clearedFilter);
    setFilter(clearedFilter);
    setFilterOpen(false);
  };

  const handleViewDetails = (execution: JobExecutionResponse) => {
    setSelectedExecution(execution);
    setDetailsOpen(true);
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
      field: 'scheduledTime',
      headerName: 'Scheduled',
      width: 180,
      valueFormatter: (value) => dayjs(value).format('MMM DD, HH:mm'),
    },
    {
      field: 'startTime',
      headerName: 'Started',
      width: 180,
      valueFormatter: (value) => (value ? dayjs(value).format('MMM DD, HH:mm:ss') : '-'),
    },
    {
      field: 'endTime',
      headerName: 'Ended',
      width: 180,
      valueFormatter: (value) => (value ? dayjs(value).format('MMM DD, HH:mm:ss') : '-'),
    },
    {
      field: 'durationSeconds',
      headerName: 'Duration',
      width: 120,
      valueFormatter: (value) => {
        if (!value) return '-';
        const dur = dayjs.duration(value, 'seconds');
        if (dur.asHours() >= 1) {
          return dur.format('H[h] m[m]');
        } else if (dur.asMinutes() >= 1) {
          return dur.format('m[m] s[s]');
        }
        return dur.format('s[s]');
      },
    },
    {
      field: 'isManualTrigger',
      headerName: 'Trigger',
      width: 100,
      renderCell: (params: GridRenderCellParams<JobExecutionResponse>) => (
        <Chip
          label={params.value ? 'Manual' : 'Scheduled'}
          size="small"
          variant="outlined"
          color={params.value ? 'secondary' : 'default'}
        />
      ),
    },
    {
      field: 'retryCount',
      headerName: 'Retries',
      width: 90,
      align: 'center',
      headerAlign: 'center',
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 100,
      sortable: false,
      renderCell: (params: GridRenderCellParams<JobExecutionResponse>) => (
        <IconButton
          size="small"
          onClick={() => handleViewDetails(params.row)}
          color="primary"
        >
          <VisibilityIcon fontSize="small" />
        </IconButton>
      ),
    },
  ];

  if (error) {
    return (
      <Alert severity="error">
        Error loading job history: {error instanceof Error ? error.message : 'Unknown error'}
      </Alert>
    );
  }

  return (
    <LocalizationProvider dateAdapter={AdapterDayjs}>
      <Box>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Typography variant="h4">Job Execution History</Typography>
          <Box>
            <Button
              startIcon={filterOpen ? <ExpandLessIcon /> : <FilterListIcon />}
              onClick={() => setFilterOpen(!filterOpen)}
              variant="outlined"
              sx={{ mr: 1 }}
            >
              Filters
            </Button>
            <Button startIcon={<RefreshIcon />} onClick={() => refetch()} variant="outlined">
              Refresh
            </Button>
          </Box>
        </Box>

        {/* Filter Panel */}
        <Collapse in={filterOpen}>
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <Grid container spacing={2}>
                <Grid item xs={12} md={3}>
                  <TextField
                    select
                    fullWidth
                    label="Status"
                    value={tempFilter.status ?? ''}
                    onChange={(e) =>
                      setTempFilter({
                        ...tempFilter,
                        status: e.target.value ? Number(e.target.value) : undefined,
                      })
                    }
                  >
                    <MenuItem value="">All</MenuItem>
                    <MenuItem value={ExecutionStatus.Succeeded}>Succeeded</MenuItem>
                    <MenuItem value={ExecutionStatus.Failed}>Failed</MenuItem>
                    <MenuItem value={ExecutionStatus.TimedOut}>Timed Out</MenuItem>
                    <MenuItem value={ExecutionStatus.Cancelled}>Cancelled</MenuItem>
                    <MenuItem value={ExecutionStatus.Skipped}>Skipped</MenuItem>
                  </TextField>
                </Grid>
                <Grid item xs={12} md={3}>
                  <DatePicker
                    label="Start Date From"
                    value={tempFilter.startDateFrom ? dayjs(tempFilter.startDateFrom) : null}
                    onChange={(date: Dayjs | null) =>
                      setTempFilter({
                        ...tempFilter,
                        startDateFrom: date?.toISOString(),
                      })
                    }
                    slotProps={{ textField: { fullWidth: true } }}
                  />
                </Grid>
                <Grid item xs={12} md={3}>
                  <DatePicker
                    label="Start Date To"
                    value={tempFilter.startDateTo ? dayjs(tempFilter.startDateTo) : null}
                    onChange={(date: Dayjs | null) =>
                      setTempFilter({
                        ...tempFilter,
                        startDateTo: date?.toISOString(),
                      })
                    }
                    slotProps={{ textField: { fullWidth: true } }}
                  />
                </Grid>
                <Grid item xs={12} md={3}>
                  <TextField
                    select
                    fullWidth
                    label="Trigger Type"
                    value={tempFilter.isManualTrigger ?? ''}
                    onChange={(e) =>
                      setTempFilter({
                        ...tempFilter,
                        isManualTrigger: e.target.value === '' ? undefined : e.target.value === 'true',
                      })
                    }
                  >
                    <MenuItem value="">All</MenuItem>
                    <MenuItem value="true">Manual</MenuItem>
                    <MenuItem value="false">Scheduled</MenuItem>
                  </TextField>
                </Grid>
                <Grid item xs={12}>
                  <Box display="flex" justifyContent="flex-end" gap={1}>
                    <Button onClick={handleClearFilter}>Clear</Button>
                    <Button variant="contained" onClick={handleApplyFilter}>
                      Apply Filters
                    </Button>
                  </Box>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Collapse>

        <Card>
          <CardContent>
            {isLoading ? (
              <Box display="flex" justifyContent="center" p={4}>
                <CircularProgress />
              </Box>
            ) : (
              <DataGrid
                rows={executionsData?.items || []}
                columns={columns}
                rowCount={executionsData?.totalCount || 0}
                loading={isLoading}
                pageSizeOptions={[10, 25, 50, 100]}
                paginationMode="server"
                paginationModel={{ page, pageSize }}
                onPaginationModelChange={(model) => {
                  setPage(model.page);
                  setPageSize(model.pageSize);
                  setFilter({
                    ...filter,
                    pageNumber: model.page + 1,
                    pageSize: model.pageSize,
                  });
                }}
                autoHeight
                disableRowSelectionOnClick
              />
            )}
          </CardContent>
        </Card>

        {/* Execution Details Dialog */}
        <Dialog open={detailsOpen} onClose={() => setDetailsOpen(false)} maxWidth="md" fullWidth>
          <DialogTitle>Execution Details</DialogTitle>
          <DialogContent>
            {selectedExecution && (
              <Box>
                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <Typography variant="subtitle2" color="text.secondary">
                      Job Name
                    </Typography>
                    <Typography variant="body1">{selectedExecution.jobName}</Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="subtitle2" color="text.secondary">
                      Status
                    </Typography>
                    <Chip
                      label={getStatusLabel(selectedExecution.status)}
                      color={getStatusColor(selectedExecution.status)}
                      size="small"
                    />
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="subtitle2" color="text.secondary">
                      Start Time
                    </Typography>
                    <Typography variant="body1">
                      {selectedExecution.startTime
                        ? dayjs(selectedExecution.startTime).format('MMM DD, YYYY HH:mm:ss')
                        : '-'}
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="subtitle2" color="text.secondary">
                      End Time
                    </Typography>
                    <Typography variant="body1">
                      {selectedExecution.endTime
                        ? dayjs(selectedExecution.endTime).format('MMM DD, YYYY HH:mm:ss')
                        : '-'}
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="subtitle2" color="text.secondary">
                      Duration
                    </Typography>
                    <Typography variant="body1">
                      {selectedExecution.durationSeconds
                        ? `${selectedExecution.durationSeconds.toFixed(2)}s`
                        : '-'}
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="subtitle2" color="text.secondary">
                      Correlation ID
                    </Typography>
                    <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                      {selectedExecution.correlationId}
                    </Typography>
                  </Grid>
                  {selectedExecution.output && (
                    <Grid item xs={12}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Output
                      </Typography>
                      <Box
                        sx={{
                          bgcolor: 'grey.100',
                          p: 2,
                          borderRadius: 1,
                          fontFamily: 'monospace',
                          fontSize: '0.875rem',
                          maxHeight: 200,
                          overflow: 'auto',
                        }}
                      >
                        {selectedExecution.output}
                      </Box>
                    </Grid>
                  )}
                  {selectedExecution.errorMessage && (
                    <Grid item xs={12}>
                      <Typography variant="subtitle2" color="error">
                        Error Message
                      </Typography>
                      <Box
                        sx={{
                          bgcolor: 'error.light',
                          color: 'error.contrastText',
                          p: 2,
                          borderRadius: 1,
                          fontFamily: 'monospace',
                          fontSize: '0.875rem',
                        }}
                      >
                        {selectedExecution.errorMessage}
                      </Box>
                    </Grid>
                  )}
                </Grid>
              </Box>
            )}
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDetailsOpen(false)}>Close</Button>
          </DialogActions>
        </Dialog>
      </Box>
    </LocalizationProvider>
  );
};

export default JobHistory;
