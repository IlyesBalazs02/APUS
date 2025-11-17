import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { EditActivityDto } from './EditActivityDto';

@Component({
  selector: 'app-edit-activity',
  standalone: false,
  templateUrl: './edit-activity.component.html',
  styleUrls: ['./edit-activity.component.css'],
})
export class EditActivityComponent {
  activityId: string;

  editModel: EditActivityDto = {
    id: '',
    title: '',
    description: '',
    date: '',
    activityType: ''
  };

  // options must match your C# enum names: ActivityType.Running, Hiking, ...
  activityTypes = [
    { value: 'MainActivity', label: 'Activity' },
    { value: 'Running', label: 'Running' },
    { value: 'Hiking', label: 'Hiking' },
    { value: 'Cycling', label: 'Cycling' },
    { value: 'GpsRelatedActivity', label: 'Gps-related' },
  ];

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    private router: Router,
  ) {
    this.activityId = this.route.snapshot.paramMap.get('id')!;
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.activityId = params.get('id')!;
      this.loadActivity();
    });
  }

  private loadActivity() {
    this.http.get<any>(`/api/activities/${this.activityId}`).subscribe(dto => {
      // dto is ActivityDto from GetById → uses .type, .title, .description, .date
      const d = dto.date ? new Date(dto.date).toISOString().substring(0, 10) : '';

      this.editModel = {
        id: dto.id,
        title: dto.title,
        description: dto.description,
        date: d,
        // "Running", "Hiking", "GpsRelatedActivity", ...
        activityType: dto.type
      };
    });
  }

  submit(form: any) {
    if (form.invalid) {
      return;
    }

    const payload: EditActivityDto = {
      ...this.editModel,
      id: this.activityId, // make sure ids match
    };

    this.http.put<void>(`/api/activities/${this.activityId}`, payload)
      .subscribe({
        next: () => {
          // navigate back to details page
          this.router.navigate(['/activities', this.activityId]);
        },
        error: err => {
          console.error(err);
          alert('Failed to save changes.');
        }
      });
  }
}
