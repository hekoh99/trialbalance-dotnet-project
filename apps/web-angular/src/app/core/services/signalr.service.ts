import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { ValidationStatus } from '../models/validation-result.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hubConnection?: signalR.HubConnection;

  /**
   * Connect to the validation hub and subscribe to a single engagement's status
   * updates. Returns an Observable that completes when the caller unsubscribes;
   * teardown also stops the hub connection to avoid leaking WebSockets.
   */
  connect(engagementId: string): Observable<ValidationStatus> {
    return new Observable<ValidationStatus>(observer => {
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${environment.apiUrl}/hubs/validation`)
        .withAutomaticReconnect()
        .build();

      this.hubConnection.on('ValidationStatusUpdated', (data: ValidationStatus) => {
        observer.next(data);
      });

      this.hubConnection
        .start()
        .then(() => this.hubConnection!.invoke('JoinEngagement', engagementId))
        .catch(err => observer.error(err));

      return () => {
        this.hubConnection?.stop().catch(() => {});
      };
    });
  }
}
