import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TransportationService, Trip } from '../../services/transportation.service';

@Component({
  selector: 'app-transportation',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './transportation.component.html',
  styleUrl: './transportation.component.css'
})
export class TransportationComponent implements OnInit {
  trips: Trip[] = [];
  loading = false;
  saving = false;
  error = '';
  showAddForm = false;
  newTrip = {
    referenceName: '',
    project: '',
    route: '',
    tripType: '',
    tripDate: ''
  };

  constructor(private readonly transportationService: TransportationService) {}

  ngOnInit() {
    this.loadTrips();
  }

  loadTrips() {
    this.loading = true;
    this.error = '';
    this.transportationService.getTrips().subscribe({
      next: (trips) => {
        this.trips = trips;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading trips:', err);
        this.error = 'Unable to load trips.';
        this.loading = false;
      }
    });
  }

  addTrip() {
    if (!this.newTrip.referenceName || !this.newTrip.project || !this.newTrip.route || !this.newTrip.tripType || !this.newTrip.tripDate) {
      return;
    }

    this.saving = true;
    this.transportationService.createTrip(this.newTrip).subscribe({
      next: () => {
        this.resetForm();
        this.loadTrips();
      },
      error: (err) => {
        console.error('Error adding trip:', err);
        this.error = 'Unable to add trip.';
        this.saving = false;
      }
    });
  }

  deleteTrip(trip: Trip) {
    if (!confirm(`Delete trip ${trip.tripCode}?`)) {
      return;
    }

    this.transportationService.deleteTrip(trip.id).subscribe({
      next: () => this.loadTrips(),
      error: (err) => {
        console.error('Error deleting trip:', err);
        this.error = 'Unable to delete trip.';
      }
    });
  }

  toggleForm() {
    this.showAddForm = !this.showAddForm;
    if (!this.showAddForm) {
      this.resetForm();
    }
  }

  trackByTripId(_index: number, trip: Trip) {
    return trip.id;
  }

  private resetForm() {
    this.newTrip = {
      referenceName: '',
      project: '',
      route: '',
      tripType: '',
      tripDate: ''
    };
    this.showAddForm = false;
    this.saving = false;
  }
}
