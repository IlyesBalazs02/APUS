import { FormlyFieldConfig } from '@ngx-formly/core';

export const mainActivityFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [

        ]
    }

]

export const mainFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [
            {
                key: 'title',
                type: 'input',
                className: 'flex-1',
                templateOptions: {
                    label: 'Title',
                    required: true,
                },
            },
            {
                key: 'description',
                type: 'textarea',
                className: 'flex-1',
                templateOptions: {
                    label: 'Description',
                },
            },
            {
                key: 'date',
                type: 'input',
                className: 'flex-1',
                templateOptions: {
                    type: 'date',
                    label: 'Date',
                    required: true,
                },
            },
            {
                key: 'duration',
                type: 'input',
                className: 'flex-1',
                templateOptions: {
                    type: 'time',
                    label: 'Duration (hh:mm:ss)',
                    required: true,
                }
            }
        ]
    },
    {
        template: '<hr /><div><strong>Additional:</strong></div>',
    },
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [
            {
                key: 'calories',
                type: 'input',
                className: 'flex-2',
                templateOptions: {
                    type: 'number',
                    label: 'Calories',
                    min: 0,
                },
            },
            {
                key: 'avgHeartRate',
                type: 'input',
                className: 'flex-2',
                templateOptions: {
                    type: 'number',
                    label: 'Average Heart Rate (bpm)',
                    min: 0,
                },
            },
            {
                key: 'maxHeartRate',
                type: 'input',
                className: 'flex-2',
                templateOptions: {
                    type: 'number',
                    label: 'Maximum Heart Rate (bpm)',
                    min: 0,
                },
            }
        ]
    },
];

export const runningFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [
            {
                key: 'AvgPace',
                type: 'input',
                templateOptions: {
                    type: 'time',
                    label: 'Pace (mm:ss)',
                },
            },
            {
                key: 'TotalDistanceKm',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Distance (km)',
                    min: 0,
                },
            },
            {
                key: 'TotalAscentMeters',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Elevation Gain (m)',
                    min: 0,
                },
            },
        ]
    }
];

export const boulderingFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [
            {
                key: 'difficulty',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Difficulty',
                    min: 0,
                },
            }
        ]
    }
];

export const rockClimbingFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [
            {
                key: 'difficulty',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Difficulty',
                    min: 0,
                },
            },
            {
                key: 'elevationGain',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Elevation Gain (meter)',
                    min: 0,
                },
            }
        ]
    }
];

export const hikingFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [
            {
                key: 'distance',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Distance (km)',
                    min: 0,
                },
            },
            {
                key: 'elevationGain',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Elevation Gain (meter)',
                    min: 0,
                },
            }
        ]
    }
];

export const yogaFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [

        ]
    }
];

export const footballFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [
            {
                key: 'distance',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Distance (km)',
                    min: 0,
                },
            }
        ]
    }
];

export const walkFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [
            {
                key: 'distance',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Distance (km)',
                    min: 0,
                },
            }
        ]
    }
];

export const rideFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [
            {
                key: 'distance',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Distance (km)',
                    min: 0,
                },
            }
        ]
    }
];

export const swimmingFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [
            {
                key: 'distance',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Distance (km)',
                    min: 0,
                },
            }
        ]
    }
];

export const skiFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [
            {
                key: 'distance',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Distance (km)',
                    min: 0,
                },
            },
            {
                key: 'elevaion',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Elevation',
                    min: 0,
                },
            }
        ]
    }
];

export const tennisFields: FormlyFieldConfig[] = [
    {
        fieldGroupClassName: 'display-flex',
        fieldGroup: [

        ]
    }
];
