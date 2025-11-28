import { Box, Typography, Alert, Card, CardContent } from '@mui/material';

const Dependencies = () => {
  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Job Dependencies
      </Typography>
      <Card>
        <CardContent>
          <Alert severity="info">
            Job dependency visualization and management coming soon...
            <br />
            This will include DAG visualization, dependency graph editing, and dependency analysis.
          </Alert>
        </CardContent>
      </Card>
    </Box>
  );
};

export default Dependencies;
