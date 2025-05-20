export interface ActivityDto {
    id: string;
    title: string;
    description?: string;
    duration: string;        // or number, whatever you use
    date: string;
    avgHr?: number;
    totalCalories?: number;
    type: string;            // e.g. "RunningActivityDto", "HikingActivityDto", etc.
    displayname?: string;

    // gps fields (only present on some)
    distanceKm?: number;
    elevationGain?: number;
    pace?: number;           // only on running

    likescount?: number;
}

export interface DisplayProp {
    label: string;
    value: string | number;
}
