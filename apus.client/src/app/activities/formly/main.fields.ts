import { FormlyFieldConfig } from '@ngx-formly/core';

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
                templateOptions: {
                    type: 'number',
                    label: 'Calories',
                    min: 0,
                },
            },
            {
                key: 'avgHeartRate',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Average Heart Rate (bpm)',
                    min: 0,
                },
            },
            {
                key: 'maxHeartRate',
                type: 'input',
                templateOptions: {
                    type: 'number',
                    label: 'Maximum Heart Rate (bpm)',
                    min: 0,
                },
            }
        ]
    }
];