import { Component } from '@angular/core';
import { SentimentService } from './services/sentiment.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'Sentiment Analyzer';
  userInput: string = '';
  sentimentResult: string | null = null;

  constructor(private sentimentService: SentimentService) {}

  analyzeSentiment() {
    this.sentimentService.analyzeSentiment(this.userInput).subscribe(
      response => {
        this.sentimentResult = response.sentiment;
      },
      error => {
        console.error('Error analyzing sentiment:', error);
        this.sentimentResult = 'Error analyzing sentiment. Please try again.';
      }
    );
  }
}