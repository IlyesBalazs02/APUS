export interface ActivityDto {
    id: string;
    title: string;
    description?: string;
    duration: string;
    date: string;
    avgHr?: number;
    totalCalories?: number;
    type: string;
    displayname?: string;

    // gps fields 
    distanceKm?: number;
    elevationGain?: number;
    pace?: number;

    likescount?: number;
}

export interface DisplayProp {
    label: string;
    value: string | number;
}
