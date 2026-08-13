# FitHub synthetic parking simulator

This visual grid is a development simulator. Clicking a space creates a synthetic detection event; it is **not real computer vision** and makes no claim to analyze camera images. The browser talks to the local simulator proxy, which authenticates against the independent FitHub Parking API. A future OpenCV service can call the same endpoint without changing the API.

Set the variables from `.env.example`, then run `npm start` and open `http://localhost:4300`. Available and Occupied alternate on clicks; Unknown becomes Occupied; Disabled spaces require an administrator correction in FitHub. The confidence slider controls the reported confidence. Run `npm test` to verify event mapping.
