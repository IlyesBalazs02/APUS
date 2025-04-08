export class MainActivity {
    $type!: string;
    time: number = 0;
    heartRate: number = 0;
    date: string = new Date().toISOString();
    DisplayName: string;

    constructor() {
        this.$type = 'APUS.Server.Models.Activities.MainActivity, APUS.Server';
        this.DisplayName = 'Activity';
    }
}

export class Running extends MainActivity {
    pace: number = 0;
    distance: number = 0;

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Running, APUS.Server';
        this.DisplayName = 'Running';
    }

}

export class Bouldering extends MainActivity {
    difficulty: number = 0;
    redPoint: boolean = false;

    constructor() {
        super();
        this.$type = 'APUS.Server.Models.Activities.Bouldering, APUS.Server';
        this.DisplayName = 'Bouldering';
    }
}

