using System;
using System.Collections.Generic;
using System.Text;


namespace Visus.Stl.Maths {

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <para>Ported from
    /// https://github.com/ServiceNow/stl-decomp-4j/blob/master/stl-decomp-4j/src/main/java/com/github/servicenow/ds/stats/stl/LoessSmoother.java
    /// </para>
    /// </remarks>
    public sealed class LoessSmoother {

        #region Public constructors
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="jump"></param>
        /// <param name="degree"></param>
        /// <param name="data"></param>
        /// <param name="externalWeights"></param>
        public LoessSmoother(int width, int jump, int degree,
                IList<double> data, IList<double> externalWeights) {
            this.Interpolator = new LoessInterpolatorBuilder()
                .SetDegree(degree)
                .SetExternalWeights(externalWeights)
                .SetWidth(width)
                .Build(data);
            this.Jump = Math.Min(Jump, data.Count - 1);
            this.Smoothed = new double[data.Count];
        }
        #endregion

        #region Public properties
        /// <summary>
        /// Gets the input data.
        /// </summary>
        public IList<double> Data => this.Interpolator.Data;

        /// <summary>
        /// Gets the interpolator used.
        /// </summary>
        public LoessInterpolatorBase Interpolator {
            get;
        }

        /// <summary>
        /// Gets the width of the jump window.
        /// </summary>
        public int Jump {
            get;
        }

        /// <summary>
        /// Gets the smoothed result.
        /// </summary>
        public IList<double> Smoothed {
            get;
        }

        /// <summary>
        /// Gets the width of the underlying interpolator.
        /// </summary>
        public int Width => this.Interpolator.Width;
        #endregion

        #region Public methods
        /// <summary>
        /// Compute LOESS-smothed data for <see cref="Data"/>.
        /// </summary>
        /// <returns></returns>
        public IList<double> Smooth() {
            if (this.Data.Count == 1) {
                this.Smoothed[0] = this.Data[0];

            } else {
                int left = -1;
                int right = -1;

                if (this.Width >= this.Data.Count) {
                    left = 0;
                    right = this.Data.Count - 1;
                    for (int i = 0; i < this.Data.Count; i += this.Jump) {
                        var y = this.Interpolator.Smooth(i, left, right);
                        this.Smoothed[i] = y ?? this.Data[i];
                    }

                } else if (this.Jump == 1) {
                    int halfWidth = (this.Width + 1) / 2;
                    left = 0;
                    right = this.Width - 1;
                    for (int i = 0; i < this.Data.Count; ++i) {
                        if ((i >= halfWidth) && (right != this.Data.Count - 1)) {
                            ++left;
                            ++right;
                        }
                        var y = this.Interpolator.Smooth(i, left, right);
                        this.Smoothed[i] = y ?? this.Data[i];
                        // logSmoothedPoint(i, smooth[i]);
                    }
                } else {
                    // For reference, the original RATFOR:
                    // else { # newnj greater than one, len less than n
                    // nsh = (len+1)/2
                    // do i = 1,n,newnj { # fitted value at i
                    // if(i<nsh) {              // i     = [1, 2, 3, 4, 5, 6, 7, 8, 9]; 9 points
                    // nleft = 1                // left  = [1, 1, 1, 1, 1, 1, 1, 1, 1];
                    // nright = len             // right = [19, 19, 19, 19, 19, 19, 19, 19, 19]; right - left = 18
                    // }
                    // else if(i>=n-nsh+1) {    // i     = [135, 136, 137, 138, 139, 140, 141, 142, 143, 144]; 10 points
                    // nleft = n-len+1          // left  = [126, 126, 126, 126, 126, 126, 126, 126, 126, 126];
                    // nright = n               // right = [144, 144, 144, 144, 144, 144, 144, 144, 144, 144]; right - left = 18
                    // }
                    // else {                   // i     = [10, 11, 12, ..., 132, 133, 134]; 125 points
                    // nleft = i-nsh+1          // left  = [1, 2, 3, ..., 123, 124, 125]
                    // nright = len+i-nsh       // right = [19, 20, 21, ..., 141, 142, 143]; right - left = 18
                    // }
                    // call est(y,n,len,ideg,float(i),ys(i),nleft,nright,res,userw,rw,ok)
                    // if(!ok) ys(i) = y(i)
                    // }
                    // }
                    // Note that RATFOR/Fortran are indexed from 1
                    //
                    // test: data.length == 144, fWidth = 19
                    //   --> halfWidth = 10
                    // Ignoring jumps...
                    // First branch for  i = [0, 1, 2, 3, 4, 5, 6, 7, 8]; 9 points
                    //                left = [0, 0, 0, 0, 0, 0, 0, 0, 0]
                    //               right = [18, 18, 18, 18, 18, 18, 18, 18, 18]; right - left = 18
                    // Second branch for i = [134, 135, 136, 137, 138, 139, 140, 141, 142, 143]; 10 points
                    //                left = [125, 125, 125, 125, 125, 125, 125, 125, 125, 125];
                    //               right = [143, 143, 143, 143, 143, 143, 143, 143, 143, 143]; right - left = 18
                    // Third branch for  i = [ 9, 10, 11, ..., 131, 132, 133]; 125 points
                    //                left = [ 0,  1,  2, ..., 122, 123, 124]
                    //               right = [18, 19, 20, ..., 140, 141, 142]; right - left = 18
                    int halfWidth = (this.Width + 1) / 2;
                    for (int i = 0; i < this.Data.Count; i += this.Jump) {
                        if (i < halfWidth - 1) {
                            left = 0;
                        } else if (i >= this.Data.Count - halfWidth) {
                            left = this.Data.Count - this.Width;
                        } else {
                            left = i - halfWidth + 1;
                        }
                        right = left + this.Width - 1;
                        var y = this.Interpolator.Smooth(i, left, right);
                        this.Smoothed[i] = y ?? this.Data[i];
                        // logSmoothedPoint(i, smooth[i]);
                    }
                }

                if (this.Jump != 1) {
                    for (int i = 0; i < this.Data.Count - this.Jump; i += this.Jump) {
                        double slope = (this.Smoothed[i + this.Jump] - this.Smoothed[i]) / (double) this.Jump;
                        for (int j = i + 1; j < i + this.Jump; ++j) {
                            this.Smoothed[j] = this.Smoothed[i] + slope * (j - i);
                            // logInterpolatedPoint(j, smooth[j]);
                        }
                    }

                    int last = this.Data.Count - 1;
                    int lastSmoothedPos = (last / this.Jump) * this.Jump;
                    if (lastSmoothedPos != last) {
                        var y = this.Interpolator.Smooth(last, left, right);
                        this.Smoothed[last] = y == null ? this.Data[last] : y.Value;
                        // logSmoothedPoint(last, smooth[last]);

                        if (lastSmoothedPos != last - 1) {
                            double slope = (this.Smoothed[last] - this.Smoothed[lastSmoothedPos]) / (last - lastSmoothedPos);
                            for (int j = lastSmoothedPos + 1; j < last; ++j) {
                                this.Smoothed[j] = this.Smoothed[lastSmoothedPos] + slope * (j - lastSmoothedPos);
                                // logInterpolatedPoint(j, smooth[j]);
                            }
                        }
                    }
                }
            }

            return this.Smoothed;
        }
        #endregion

    }
}
