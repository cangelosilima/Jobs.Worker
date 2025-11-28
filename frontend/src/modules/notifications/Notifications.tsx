import { Box, Typography, Alert, Card, CardContent } from '@mui/material';

const Notifications = () => {
  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Notifications Center
      </Typography>
      <Card>
        <CardContent>
          <Alert severity="info">
            Notification center coming soon...
            <br />
            This will include notification configuration, history, and preferences.
          </Alert>
        </CardContent>
      </Card>
    </Box>
  );
};

export default Notifications;
