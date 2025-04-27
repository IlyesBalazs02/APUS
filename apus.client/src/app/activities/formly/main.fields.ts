import { FormlyFieldConfig } from '@ngx-formly/core';

export const mainFields: FormlyFieldConfig[] = [
    {
        key: 'title',
        type: 'input',
        className: 'col-md-4 mb-3',        // makes this field 1/3 width on md+ screens
        templateOptions: {
            label: 'Title',
            required: true,
        },
    },
    {
        key: 'description',
        type: 'textarea',
        className: 'col-md-2',             // narrower width
        templateOptions: {
            label: 'Description',
            rows: 3,
        },
    },
    {
        key: 'date',
        type: 'input',
        className: 'col-md-2',             // narrower width
        templateOptions: {
            type: 'date',
            label: 'Date',
            required: true,
        },
    },
    {
        key: 'duration',
        type: 'input',
        templateOptions: {
            type: 'time',
            label: 'Duration (hh:mm:ss)',
            required: true,
        },
    },
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
    },

];