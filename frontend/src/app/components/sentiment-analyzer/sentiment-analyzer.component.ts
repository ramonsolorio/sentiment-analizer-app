import { Component } from '@angular/core';
import { SentimentService } from '../../services/sentiment.service';
import { SentimentResponse } from '../../models/sentiment-response.model';

@Component({
  selector: 'app-sentiment-analyzer',
  templateUrl: './sentiment-analyzer.component.html',
  styleUrls: ['./sentiment-analyzer.component.css']
})
export class SentimentAnalyzerComponent {
  userInput: string = '';
  sentimentResponse: SentimentResponse | null = null;

  constructor(private sentimentService: SentimentService) {}

  analyzeSentiment() {
    if (this.userInput.trim()) {
      this.sentimentService.analyzeSentiment(this.userInput).subscribe(
        (response) => {
          this.sentimentResponse = response;
        },
        (error) => {
          console.error('Error analyzing sentiment:', error);
          this.sentimentResponse = null;
        }
      );
    }
  }
}