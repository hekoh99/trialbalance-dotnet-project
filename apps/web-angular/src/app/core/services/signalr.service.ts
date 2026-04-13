import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { ValidationStatus } from '../models/validation-result.model';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hubConnection!: signalR.HubConnection;

  connect(engagementId: string): Observable<ValidationStatus> {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5211/hubs/validation')
      .withAutomaticReconnect()
      .build();

    return new Observable(observer => {
      this.hubConnection.on('ValidationStatusUpdated', (data: ValidationStatus) => {
        observer.next(data);
      });

      this.hubConnection.start()
        .then(() => this.hubConnection.invoke('JoinEngagement', engagementId))
        .catch(err => observer.error(err));

      return () => {
        this.hubConnection.stop();
      };
    });
  }
}
