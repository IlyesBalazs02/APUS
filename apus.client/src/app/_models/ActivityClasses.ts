export class MainActivity {
    $type!: string;
    time: number = 0; // Updated to match API response
    heartRate: number = 0; // Updated to match API response
    date: string = new Date().toISOString(); // Updated to match API response
    displayName: string;

    constructor() {
        this.$type = 'APUS.Server.Models.Activities.MainActivity, APUS.Server';
        this.displayName = 'Activity';
    }
}

export class Running extends MainActivity {
    pace: number = 0; // Updated to match API response
    distance: number = 0; // Updated to match API response

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Running, APUS.Server';
        this.displayName = 'Running';
    }
}

export class Bouldering extends MainActivity {
    difficulty: number = 0; // Updated to match API response
    redPoint: number = 0; // Updated to match API response

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Bouldering, APUS.Server';
        this.displayName = 'Bouldering';
    }
}