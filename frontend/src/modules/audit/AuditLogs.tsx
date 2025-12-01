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
} from '@mui/material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import { Refresh as RefreshIcon } from '@mui/icons-material';
import { auditApi } from '@/api/jobs.api';
import { signalRService, AuditLogEntry } from '@/services/signalr';
import { AuditLogResponse } from '@/api/types';
import dayjs from 'dayjs';

const AuditLogs = () => {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(25);
  const [localLogs, setLocalLogs] = useState<AuditLogResponse[]>([]);

  const {
    data: logsData,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['auditLogs', page, pageSize],
    queryFn: () =>
      auditApi.getAll({
        pageNumber: page + 1,
        pageSize: pageSize,
      }),
  });

  // Initialize local state
  useEffect(() => {
    if (logsData?.items) {
      setLocalLogs(logsData.items);
    }
  }, [logsData]);

  // Subscribe to real-time audit log updates
  useEffect(() => {
    const handleAuditLogAdded = (entry: AuditLogEntry) => {
      const newLog: AuditLogResponse = {
        id: entry.id,
        timestamp: entry.timestamp,
        userId: entry.userId,
        userName: entry.userName,
        action: entry.action,
        entityType: entry.entityType,
        entityId: entry.entityId,
        before: null,
        after: null,
        changes: entry.changes,
      };

      setLocalLogs((prev) => [newLog, ...prev].slice(0, pageSize));
    };

    signalRService.on('AuditLogAdded', handleAuditLogAdded);

    return () => {
      signalRService.off('AuditLogAdded', handleAuditLogAdded);
    };
  }, [pageSize]);

  const columns: GridColDef[] = [
    {
      field: 'timestamp',
      headerName: 'Timestamp',
      width: 200,
      valueFormatter: (params) => dayjs(params.value).format('MMM DD, YYYY HH:mm:ss'),
    },
    {
      field: 'userName',
      headerName: 'User',
      width: 150,
    },
    {
      field: 'action',
      headerName: 'Action',
      width: 150,
      renderCell: (params: GridRenderCellParams<AuditLogResponse>) => (
        <Chip label={params.value} size="small" color="primary" variant="outlined" />
      ),
    },
    {
      field: 'entityType',
      headerName: 'Entity Type',
      width: 150,
    },
    {
      field: 'entityId',
      headerName: 'Entity ID',
      width: 150,
      renderCell: (params: GridRenderCellParams<AuditLogResponse>) => (
        <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
          {params.value.substring(0, 8)}...
        </Typography>
      ),
    },
    {
      field: 'changes',
      headerName: 'Changes',
      flex: 1,
      minWidth: 200,
      renderCell: (params: GridRenderCellParams<AuditLogResponse>) => (
        <Typography
          variant="body2"
          sx={{
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          {params.value || '-'}
        </Typography>
      ),
    },
  ];

  if (error) {
    return (
      <Alert severity="error">
        Error loading audit logs: {error instanceof Error ? error.message : 'Unknown error'}
      </Alert>
    );
  }

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4">Audit Logs</Typography>
        <Box>
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
          ) : (
            <>
              <Box mb={2}>
                <Typography variant="body2" color="text.secondary">
                  Audit log entries • Real-time updates enabled
                </Typography>
                {signalRService.isConnected() && (
                  <Chip label="Live" color="success" size="small" sx={{ ml: 1 }} />
                )}
              </Box>
              <DataGrid
                rows={localLogs}
                columns={columns}
                rowCount={logsData?.totalCount || 0}
                loading={isLoading}
                pageSizeOptions={[10, 25, 50, 100]}
                paginationMode="server"
                paginationModel={{ page, pageSize }}
                onPaginationModelChange={(model) => {
                  setPage(model.page);
                  setPageSize(model.pageSize);
                }}
                autoHeight
                disableRowSelectionOnClick
              />
            </>
          )}
        </CardContent>
      </Card>
    </Box>
  );
};

export default AuditLogs;
