export class MainActivity {
    $type!: string;
    id: string = '';
    title: string = '';
    description: string | null = null;
    date: string = new Date().toISOString(); // Updated to match API response
    duration: string | null = null;
    calories: number | null = null;
    avgHeartRate: number | null = null;
    maxHeartRate: number | null = null;
    displayName: string;
    activityType: string = ''; // required for dynamic rendering

    constructor() {
        this.$type = 'APUS.Server.Models.Activities.MainActivity, APUS.Server';
        this.displayName = 'Activity';
    }
}

export class Running extends MainActivity {
    pace: number | null = null; // Updated to match API response
    distance: number | null = null; // Updated to match API response

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Running, APUS.Server';
        this.displayName = 'Running';
        this.activityType = 'Running'; // required for dynamic rendering
    }
}

export class Bouldering extends MainActivity {
    difficulty: number | null = null;
    redPoint: boolean | null = null;

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Bouldering, APUS.Server';
        this.displayName = 'Bouldering';
    }
}
