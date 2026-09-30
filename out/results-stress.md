## BackgroundsOff

| Model | Run | Pages | Score | Backgrounds kept |
|---|---|---|---|---|
| baseline | article-1-template | 2 | 1/1 | pass (fills on 25.67% of the page, all kept) |
| baseline | control-no-color-adjust | 2 | 0/1 | FAIL (fills on 25.67% of the page, 26.60% of pixels lost them) |
| baseline | control-no-row-break | 2 | 1/1 | pass (fills on 25.67% of the page, all kept) |
| fable-5-1 | r1-01 | 3 | 0/0 | n/a |
| fable-5-1 | r1-02 | 3 | 1/1 | pass (fills on 2.65% of the page, all kept) |
| fable-5-1 | r1-03 | 3 | 1/1 | pass (fills on 2.95% of the page, all kept) |
| fable-5-1 | r1-04 | 3 | 1/1 | pass (fills on 2.96% of the page, all kept) |
| fable-5-1 | r1-05 | 3 | 1/1 | pass (fills on 2.84% of the page, all kept) |
| gpt-6-astra | r1-01 | 3 | 0/1 | FAIL (fills on 2.77% of the page, 2.77% of pixels lost them) |
| gpt-6-astra | r1-02 | 3 | 0/0 | n/a |
| gpt-6-astra | r1-03 | 3 | 0/0 | n/a |
| gpt-6-astra | r1-04 | 3 | 0/0 | n/a |
| gpt-6-astra | r1-05 | 3 | 0/0 | n/a |

## LongRow

| Model | Run | Pages | Score | Header repeats | Totals together | Rows intact | Long row intact |
|---|---|---|---|---|---|---|---|
| baseline | article-1-template | 3 | 4/4 | pass (on pages 2) | pass (subtotal on page 3, total on page 3) | pass (all 47 rows) | pass (whole on page 2) |
| baseline | control-no-color-adjust | 3 | 4/4 | pass (on pages 2) | pass (subtotal on page 3, total on page 3) | pass (all 47 rows) | pass (whole on page 2) |
| baseline | control-no-row-break | 2 | 3/4 | pass (on pages 2) | pass (subtotal on page 2, total on page 2) | pass (all 47 rows) | FAIL (starts on page 1, ends on page 2) |
| fable-5-1 | r1-01 | 3 | 4/4 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 47 rows) | pass (whole on page 2) |
| fable-5-1 | r1-02 | 3 | 4/4 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 47 rows) | pass (whole on page 2) |
| fable-5-1 | r1-03 | 3 | 4/4 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 47 rows) | pass (whole on page 2) |
| fable-5-1 | r1-04 | 3 | 4/4 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 47 rows) | pass (whole on page 2) |
| fable-5-1 | r1-05 | 3 | 4/4 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 47 rows) | pass (whole on page 2) |
| gpt-6-astra | r1-01 | 3 | 4/4 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 47 rows) | pass (whole on page 2) |
| gpt-6-astra | r1-02 | 3 | 4/4 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 47 rows) | pass (whole on page 2) |
| gpt-6-astra | r1-03 | 3 | 4/4 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 47 rows) | pass (whole on page 2) |
| gpt-6-astra | r1-04 | 3 | 4/4 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 47 rows) | pass (whole on page 2) |
| gpt-6-astra | r1-05 | 3 | 4/4 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 47 rows) | pass (whole on page 2) |

## ManyLines

| Model | Run | Pages | Score | Header repeats | Totals together | Rows intact |
|---|---|---|---|---|---|---|
| baseline | article-1-template | 7 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7) | pass (subtotal on page 7, total on page 7) | pass (all 200 rows) |
| baseline | control-no-color-adjust | 7 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7) | pass (subtotal on page 7, total on page 7) | pass (all 200 rows) |
| baseline | control-no-row-break | 7 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7) | pass (subtotal on page 7, total on page 7) | pass (all 200 rows) |
| fable-5-1 | r1-01 | 9 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7, 8, 9) | pass (subtotal on page 9, total on page 9) | pass (all 200 rows) |
| fable-5-1 | r1-02 | 9 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7, 8) | pass (subtotal on page 9, total on page 9) | pass (all 200 rows) |
| fable-5-1 | r1-03 | 9 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7, 8, 9) | pass (subtotal on page 9, total on page 9) | pass (all 200 rows) |
| fable-5-1 | r1-04 | 9 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7, 8, 9) | pass (subtotal on page 9, total on page 9) | pass (all 200 rows) |
| fable-5-1 | r1-05 | 9 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7, 8, 9) | pass (subtotal on page 9, total on page 9) | pass (all 200 rows) |
| gpt-6-astra | r1-01 | 10 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7, 8, 9, 10) | pass (subtotal on page 10, total on page 10) | pass (all 200 rows) |
| gpt-6-astra | r1-02 | 10 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7, 8, 9, 10) | pass (subtotal on page 10, total on page 10) | pass (all 200 rows) |
| gpt-6-astra | r1-03 | 10 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7, 8, 9, 10) | pass (subtotal on page 10, total on page 10) | pass (all 200 rows) |
| gpt-6-astra | r1-04 | 10 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7, 8, 9, 10) | pass (subtotal on page 10, total on page 10) | pass (all 200 rows) |
| gpt-6-astra | r1-05 | 10 | 3/3 | pass (on pages 2, 3, 4, 5, 6, 7, 8, 9, 10) | pass (subtotal on page 10, total on page 10) | pass (all 200 rows) |

## EuroLongNames

| Model | Run | Pages | Score | Header repeats | Totals together | Rows intact |
|---|---|---|---|---|---|---|
| baseline | article-1-template | 2 | 3/3 | pass (on pages 2) | pass (subtotal on page 2, total on page 2) | pass (all 46 rows) |
| baseline | control-no-color-adjust | 2 | 3/3 | pass (on pages 2) | pass (subtotal on page 2, total on page 2) | pass (all 46 rows) |
| baseline | control-no-row-break | 2 | 3/3 | pass (on pages 2) | pass (subtotal on page 2, total on page 2) | pass (all 46 rows) |
| fable-5-1 | r1-01 | 3 | 3/3 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 46 rows) |
| fable-5-1 | r1-02 | 3 | 3/3 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 46 rows) |
| fable-5-1 | r1-03 | 3 | 3/3 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 46 rows) |
| fable-5-1 | r1-04 | 3 | 3/3 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 46 rows) |
| fable-5-1 | r1-05 | 3 | 3/3 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 46 rows) |
| gpt-6-astra | r1-01 | 3 | 3/3 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 46 rows) |
| gpt-6-astra | r1-02 | 3 | 3/3 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 46 rows) |
| gpt-6-astra | r1-03 | 3 | 3/3 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 46 rows) |
| gpt-6-astra | r1-04 | 3 | 3/3 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 46 rows) |
| gpt-6-astra | r1-05 | 3 | 3/3 | pass (on pages 2, 3) | pass (subtotal on page 3, total on page 3) | pass (all 46 rows) |

