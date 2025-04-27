// running.fields.ts
import { FormlyFieldConfig } from '@ngx-formly/core';
import { mainFields } from './main.fields';

export const runningFields: FormlyFieldConfig[] = [
    {
        key: 'pace',
        type: 'input',
        templateOptions: {
            type: 'time',
            label: 'Pace (mm:ss)',
        },
    },
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
            label: 'Elevation Gain (m)',
            min: 0,
        },
    },
];
