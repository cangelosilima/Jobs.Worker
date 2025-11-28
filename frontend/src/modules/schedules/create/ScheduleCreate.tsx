import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Button,
  TextField,
  MenuItem,
  Grid,
  Alert,
  Stepper,
  Step,
  StepLabel,
  FormControlLabel,
  Checkbox,
} from '@mui/material';
import { DatePicker, TimePicker } from '@mui/x-date-pickers';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { jobsApi, schedulesApi } from '@/api/jobs.api';
import { RecurrenceType, CreateScheduleCommand } from '@/api/types';
import dayjs, { Dayjs } from 'dayjs';

const steps = ['Select Job', 'Configure Schedule', 'Review'];

const daysOfWeek = [
  { value: 0, label: 'Sunday' },
  { value: 1, label: 'Monday' },
  { value: 2, label: 'Tuesday' },
  { value: 3, label: 'Wednesday' },
  { value: 4, label: 'Thursday' },
  { value: 5, label: 'Friday' },
  { value: 6, label: 'Saturday' },
];

const ScheduleCreate = () => {
  const navigate = useNavigate();
  const [activeStep, setActiveStep] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const [formData, setFormData] = useState<CreateScheduleCommand>({
    jobId: '',
    recurrenceType: RecurrenceType.Daily,
    startDate: dayjs().format('YYYY-MM-DD'),
    timeOfDay: '09:00:00',
  });

  const [selectedDays, setSelectedDays] = useState<number[]>([]);
  const [startDate, setStartDate] = useState<Dayjs>(dayjs());
  const [endDate, setEndDate] = useState<Dayjs | null>(null);
  const [timeOfDay, setTimeOfDay] = useState<Dayjs>(dayjs().hour(9).minute(0));

  const { data: jobs, isLoading: jobsLoading } = useQuery({
    queryKey: ['allJobs'],
    queryFn: jobsApi.getAll,
  });

  const createMutation = useMutation({
    mutationFn: schedulesApi.create,
    onSuccess: () => {
      navigate('/schedules');
    },
    onError: (err: any) => {
      setError(err.message || 'Failed to create schedule');
    },
  });

  const handleNext = () => {
    if (activeStep === 0 && !formData.jobId) {
      setError('Please select a job');
      return;
    }
    setError(null);
    setActiveStep((prev) => prev + 1);
  };

  const handleBack = () => {
    setError(null);
    setActiveStep((prev) => prev - 1);
  };

  const handleSubmit = () => {
    const command: CreateScheduleCommand = {
      ...formData,
      startDate: startDate.format('YYYY-MM-DD'),
      endDate: endDate?.format('YYYY-MM-DD'),
      timeOfDay: timeOfDay.format('HH:mm:ss'),
      daysOfWeek: formData.recurrenceType === RecurrenceType.Weekly ? selectedDays : undefined,
    };

    createMutation.mutate(command);
  };

  const handleDayToggle = (day: number) => {
    setSelectedDays((prev) =>
      prev.includes(day) ? prev.filter((d) => d !== day) : [...prev, day]
    );
  };

  return (
    <LocalizationProvider dateAdapter={AdapterDayjs}>
      <Box>
        <Typography variant="h4" gutterBottom>
          Create Schedule
        </Typography>

        <Card>
          <CardContent>
            <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
              {steps.map((label) => (
                <Step key={label}>
                  <StepLabel>{label}</StepLabel>
                </Step>
              ))}
            </Stepper>

            {error && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {error}
              </Alert>
            )}

            {/* Step 1: Select Job */}
            {activeStep === 0 && (
              <Grid container spacing={3}>
                <Grid item xs={12}>
                  <TextField
                    select
                    fullWidth
                    required
                    label="Select Job"
                    value={formData.jobId}
                    onChange={(e) => setFormData({ ...formData, jobId: e.target.value })}
                    disabled={jobsLoading}
                  >
                    {jobs?.map((job) => (
                      <MenuItem key={job.id} value={job.id}>
                        {job.name} - {job.description}
                      </MenuItem>
                    ))}
                  </TextField>
                </Grid>
              </Grid>
            )}

            {/* Step 2: Configure Schedule */}
            {activeStep === 1 && (
              <Grid container spacing={3}>
                <Grid item xs={12} md={6}>
                  <TextField
                    select
                    fullWidth
                    required
                    label="Recurrence Type"
                    value={formData.recurrenceType}
                    onChange={(e) =>
                      setFormData({ ...formData, recurrenceType: Number(e.target.value) })
                    }
                  >
                    <MenuItem value={RecurrenceType.Daily}>Daily</MenuItem>
                    <MenuItem value={RecurrenceType.Weekly}>Weekly</MenuItem>
                    <MenuItem value={RecurrenceType.Monthly}>Monthly</MenuItem>
                    <MenuItem value={RecurrenceType.Cron}>Cron Expression</MenuItem>
                    <MenuItem value={RecurrenceType.OneTime}>One-time</MenuItem>
                  </TextField>
                </Grid>

                {formData.recurrenceType === RecurrenceType.Cron && (
                  <Grid item xs={12} md={6}>
                    <TextField
                      fullWidth
                      required
                      label="Cron Expression"
                      value={formData.cronExpression || ''}
                      onChange={(e) => setFormData({ ...formData, cronExpression: e.target.value })}
                      placeholder="0 9 * * *"
                      helperText="e.g., 0 9 * * * for 9:00 AM daily"
                    />
                  </Grid>
                )}

                {formData.recurrenceType === RecurrenceType.Monthly && (
                  <Grid item xs={12} md={6}>
                    <TextField
                      fullWidth
                      required
                      type="number"
                      label="Day of Month"
                      value={formData.dayOfMonth || 1}
                      onChange={(e) =>
                        setFormData({ ...formData, dayOfMonth: parseInt(e.target.value) })
                      }
                      inputProps={{ min: 1, max: 31 }}
                    />
                  </Grid>
                )}

                {formData.recurrenceType === RecurrenceType.Weekly && (
                  <Grid item xs={12}>
                    <Typography variant="subtitle2" gutterBottom>
                      Select Days of Week
                    </Typography>
                    <Box display="flex" flexWrap="wrap" gap={1}>
                      {daysOfWeek.map((day) => (
                        <FormControlLabel
                          key={day.value}
                          control={
                            <Checkbox
                              checked={selectedDays.includes(day.value)}
                              onChange={() => handleDayToggle(day.value)}
                            />
                          }
                          label={day.label}
                        />
                      ))}
                    </Box>
                  </Grid>
                )}

                {formData.recurrenceType !== RecurrenceType.Cron && (
                  <Grid item xs={12} md={6}>
                    <TimePicker
                      label="Time of Day"
                      value={timeOfDay}
                      onChange={(newValue) => newValue && setTimeOfDay(newValue)}
                      slotProps={{ textField: { fullWidth: true } }}
                    />
                  </Grid>
                )}

                <Grid item xs={12} md={6}>
                  <DatePicker
                    label="Start Date"
                    value={startDate}
                    onChange={(newValue) => newValue && setStartDate(newValue)}
                    slotProps={{ textField: { fullWidth: true, required: true } }}
                  />
                </Grid>

                <Grid item xs={12} md={6}>
                  <DatePicker
                    label="End Date (Optional)"
                    value={endDate}
                    onChange={(newValue) => setEndDate(newValue)}
                    slotProps={{ textField: { fullWidth: true } }}
                  />
                </Grid>
              </Grid>
            )}

            {/* Step 3: Review */}
            {activeStep === 2 && (
              <Grid container spacing={2}>
                <Grid item xs={12}>
                  <Typography variant="h6" gutterBottom>
                    Review Schedule Configuration
                  </Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Job
                  </Typography>
                  <Typography>{jobs?.find((j) => j.id === formData.jobId)?.name}</Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Recurrence Type
                  </Typography>
                  <Typography>
                    {RecurrenceType[formData.recurrenceType as keyof typeof RecurrenceType]}
                  </Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Start Date
                  </Typography>
                  <Typography>{startDate.format('MMM DD, YYYY')}</Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Time
                  </Typography>
                  <Typography>{timeOfDay.format('HH:mm')}</Typography>
                </Grid>
              </Grid>
            )}

            {/* Navigation Buttons */}
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 4 }}>
              <Button onClick={() => navigate('/schedules')}>Cancel</Button>
              <Box>
                {activeStep > 0 && (
                  <Button onClick={handleBack} sx={{ mr: 1 }}>
                    Back
                  </Button>
                )}
                {activeStep < steps.length - 1 ? (
                  <Button variant="contained" onClick={handleNext}>
                    Next
                  </Button>
                ) : (
                  <Button
                    variant="contained"
                    onClick={handleSubmit}
                    disabled={createMutation.isPending}
                  >
                    {createMutation.isPending ? 'Creating...' : 'Create Schedule'}
                  </Button>
                )}
              </Box>
            </Box>
          </CardContent>
        </Card>
      </Box>
    </LocalizationProvider>
  );
};

export default ScheduleCreate;
