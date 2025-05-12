import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { ActivityDto, DisplayProp } from '../../../ActivityDto/ActivityDto';

type PropMap = Record<string, { key: keyof ActivityDto, label: string }[]>;

@Component({
  selector: 'app-activity-card',
  standalone: false,
  templateUrl: './activity-card.component.html',
  styleUrls: ['./activity-card.component.css']
})
export class ActivityCardComponent implements OnChanges {
  @Input() activity!: ActivityDto;

  public displayProps: DisplayProp[] = [];

  private readonly propMap: PropMap = {
    Running: [
      { key: 'pace', label: 'Pace' },
      { key: 'distanceKm', label: 'Distance (km)' },
    ],
    GpsRelatedActivity: [
      { key: 'distanceKm', label: 'Distance (km)' },
      { key: 'elevationGain', label: 'Elevation (m)' },
    ],
    'default': [
      { key: 'avgHr', label: 'Avg HR' },
      { key: 'totalCalories', label: 'Calories' },
    ],
  };

  ngOnChanges(changes: SimpleChanges) {
    if (changes['activity'] && this.activity) {
      this.displayProps = this.buildDisplayProps(this.activity);
      this.displayProps.push()
    }
  }

  private buildDisplayProps(a: ActivityDto): DisplayProp[] {
    const entries = this.propMap[a.type] || this.propMap['default'];

    return entries
      .filter(({ key }) => a[key] != null)
      .map(({ key, label }) => ({
        label,
        value: a[key] as string | number
      }));
  }
}
