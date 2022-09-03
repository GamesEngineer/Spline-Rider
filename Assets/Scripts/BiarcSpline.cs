using System;
using System.Collections.Generic;
using UnityEngine;

public class BiarcSpline
{
    public struct Knot
    {
        public Vector2 point;
        public float angle;
        public Vector2 tangent => new Vector2( Mathf.Cos(angle), -Mathf.Sin(angle) );
        public Knot(Vector2 point, float angle)
        {
            this.point = point;
            this.angle = angle;
        }
        public static Knot Make(Vector2 point, float angle)
        {
            return new Knot(point, angle);
        }
    }

    public struct Arc
    {
        public Vector2 center;
        public float radius;
        public float startAngle;
        public float endAngle;
        public Arc(Vector2 center, float radius, float startAngle, float endAngle)
        {
            this.center = center;
            this.radius = radius;
            this.startAngle = startAngle;
            this.endAngle = endAngle;
        }
    }

    [SerializeField]
    private List<Knot> knots = new List<Knot>();

    private List<Arc> arcs = new List<Arc>();

    public BiarcSpline()
    {
        knots.Add(new Knot(new Vector2(-1.0f, 0.0f), 90f));
        knots.Add(new Knot(new Vector2(+1.0f, 0.0f), -90f));
        UpdateArcs();
    }

    public void UpdateArcs()
    {
        arcs.Clear();

        for (int i = 0; i < knots.Count - 1; i++)
        {
            Knot knot0 = knots[i];
            Knot knot1 = knots[i + 1];


        }
    }

    private float WrapAngle(float angle)
    {
        while (angle > +Mathf.PI) angle -= Mathf.PI;
        while (angle < -Mathf.PI) angle += Mathf.PI;
        return angle;
    }

    private float Sinc(float x)
    {
        if (Mathf.Abs(x) < 0.002) return 1f + x * x / 6f * (1f - x * x / 20f);
        return Mathf.Sin(x) / x;
    }

    public struct Line
    {
        public Vector2 p0;
        public Vector2 p1;

        public Line(Vector2 p0, Vector2 p1)
        {
            this.p0 = p0;
            this.p1 = p1;
        }

        public static bool Intersect(Line a, Line b, out Vector2 intersection)
        {
            Vector2 D = a.p1 - a.p0;
            Vector2 E = b.p1 - b.p0;            
            Vector2 F = a.p0 - b.p0;

            float t = 1f / (E.y * D.x - E.x * D.y);
            if (Mathf.Abs(t) < 1e-9)
            {
                intersection = Vector2.zero;
                return false; // parallel lines have no solution
            }
            else
            {
                float s = t * (E.x * F.y - E.y * F.x);
                intersection = a.p0 + (b.p0 - a.p1) * s;
                return true;
            }
        }
    }
    
    private bool ComputeBiarc(Knot knot0, Knot knot1, out Arc arc0, out Arc arc1)
    {
        arc0 = new Arc();
        arc1 = new Arc();
        Vector2 v1 = knot1.point - knot0.point;
        Vector2 v2 = (knot1.point + knot1.tangent * -100f) - (knot0.point + knot0.tangent * 100f);
        Vector2 m1 = knot0.point + v1 * 0.5f;
        Vector2 m2 = knot0.point + knot0.tangent * 100f + v2 * 0.5f;
        Line lineA = new Line(m1, m1 + new Vector2(-v1.y, v1.x));
        Line lineB = new Line(m2, m2 + new Vector2(-v2.y, v2.x));
        if (Line.Intersect(lineA, lineB, out Vector2 intersection))
        {
            arc0.center = intersection;
            arc0.radius = (knot0.point - intersection).magnitude;
            // FIXME
            arc1.center = intersection;
            arc1.radius = (knot0.point - intersection).magnitude;
            return true;
        }
        else
        {
            return false;
        }
    }
}

#if false
// Sources: common.js _compiled.js line.js bezier.js arc.js comparison.js
// ---------------------------

// Javascript support functions for http://www.redblobgames.com/articles/curved-paths/
// Copyright 2012 Red Blob Games
// License: Apache v2


// A Diagram represents an svg diagram representation of models
// (expressed as draggable svg objects).  init should be a function
// that will be called with the diagram as 'this', and it should
// initialize any additional fields needed; redraw will also be called
// with the diagram as 'this', whenever anything changes
function Diagram(root, init, redraw) {
    this.root = root;
    this.redraw = redraw;

    // Since people are dragging things around in the workspace, and
    // dragging is also used for scrolling on touch devices, disable
    // the scrolling if dragging happens inside the workspace. The
    // problem with this is that if you've zoomed in you might not be
    // able to zoom out to scroll the page anymore.
    root.on('touchmove', function() { d3.event.preventDefault(); });

    this.buildCommonParts();
    var that = this;
    setTimeout(function() { that.redrawAll(); }, 0);
    init.call(this);
}

// Constants
Diagram.kUnderlayDragRadius = 30;
Diagram.kOverlayDragRadiusByType = {'c': 9, 't': 7, 'p': 11};


// Given a d3 node, a Model object that exports a Point,
// attach mouse/touch events to the node to make it draggable.
Diagram.prototype.makeDraggable = function(node, model) {
    var that = this;
    var drag = d3.behavior.drag()
        .origin(function() { return model.get(); })
        .on('drag', function(d) {
            model.set(new Point(d3.event.x, d3.event.y));
            that.redrawAll();
        });
    node.call(drag);
}


// Convenience function for making a drag handle stay within the root
Diagram.prototype.constrainPoint = function(model) {
    // NOTE: I'd like to use .offsetWidth and .offsetHeight here but
    // Firefox doesn't set them on SVG nodes, so I instead parse the
    // viewBox attribute, which I'm using on all the SVGs on this page
    var viewBox = this.root.attr('viewBox').split(" ");
    return constrained_point(model,
                             10, parseFloat(viewBox[2]) - 10,
                             10, parseFloat(viewBox[3]) - 10);
}


// Convenience function for making a draggable circle overlay/underlay
Diagram.prototype.makeHandle = function(className, radius) {
    var prefix = className.slice(0, 1);  // "t", "p", "c", etc.
    this.root.select('.underlay').append('circle')
        .attr('class', className + " " + prefix + " invisible-draggable")
        .attr('r', Diagram.kUnderlayDragRadius);

    if (prefix == "t") {
        this.root.select(".overlay").append('path')
            .attr('class', className + " " + prefix + " draggable")
            .attr('d', ['M', 0, 0, 'l', -2*radius, 0, 'l', 2*radius, 0, 'l', 0, radius, 'l', radius*1.4, -radius, 'l', -radius*1.4, -radius, 'l', 0, radius, 'Z'].join(" "));
    } else {
        this.root.select('.overlay').append('circle')
            .attr('class', className + " " + prefix + " draggable")
            .attr('r', radius);
    }
}


// Convenience function for the most common uses of makeDraggable
Diagram.prototype.attachDragHandleToModels = function(names) {
    var that = this;
    names.split(" ").forEach(function(name) {
        that.makeHandle(name, Diagram.kOverlayDragRadiusByType[name.slice(0, 1)]);
            
        if (name == 'p1' || name == 'p2' || name == 'c1' || name == 'c2') {
            that.makeDraggable(that.root.selectAll("." + name),
                               that.constrainPoint(that[name]));
        } else if (name == 't1' || name == 't2') {
            var angle_model = (name == 't1')? 'a1' : 'a2';
            var point_model = (name == 't1')? 'p1' : 'p2';
            that.makeDraggable(that.root.selectAll("." + name),
                               Model.Polar(Model.constant(40),
                                           Model.ref(that, angle_model))
                      .offset(that[point_model]));
            
        } else {
            throw "attachDragHandleToModel called with an unrecognized name";
        }
    });
}


// These SVG elements are common to most diagrams and therefore
// are created here instead of being written in each HTML file
Diagram.prototype.buildCommonParts = function() {
    this.root.insert('g', ':first-child')
        .attr('class', 'underlay');
    this.root.append('g')
        .attr('class', 'overlay');
    
    var defs = this.root.insert('defs', ':first-child');
    var marker = defs.append('marker')
        .attr('id', 'arrowhead')
        .attr('viewBox', "0 0 10 10")
        .attr('refX', 7)
        .attr('refY', 5)
        .attr('markerUnits', 'strokeWidth')
        .attr('markerWidth', 4)
        .attr('markerHeight', 3)
        .attr('orient', 'auto');
    var path = marker.append('path')
        .attr('d', "M 0 0 L 10 5 L 0 10 z");
}


// The generic drawer will use hard-coded class and model names
//   p1,p2,c1,c2 are shapes backed by points p1,p2,c1,c2
//   t1,t2 are shapes backed by angles a1,a2 and are linked to p1,p2
Diagram.prototype.redrawAll = function() {
    var t1 = Point.fromPolar(1, this.a1);
    var t2 = Point.fromPolar(1, this.a2);
    
    this.root.selectAll('.cross-vector')
        .attr('d', ['M', this.p1, 'L', this.p2].join(" "));

    if (this.p1) {
        this.root.selectAll('.p1')
            .attr('transform', "translate(" + this.p1 + ")");
    }
    if (this.p1 && this.a1) {
        this.root.selectAll('.t1')
            .attr('transform', "translate(" + this.p1.add(t1.scale(40)) + ") rotate(" + (this.a1 * 180 / Math.PI) + ")");
    }

    if (this.p2) {
        this.root.selectAll('.p2')
            .attr('transform', "translate(" + this.p2 + ")");
    }
    if (this.p2 && this.a2) {
        this.root.selectAll('.t2')
            .attr('transform', "translate(" + this.p2.add(t2.scale(40)) + ") rotate(" + (this.a2 * 180 / Math.PI) + ")");
    }

    if (this.c1) {
        this.root.selectAll('.c1')
            .attr('transform', "translate(" + this.c1 + ")");
    }
    if (this.c2) {
        this.root.selectAll('.c2')
            .attr('transform', "translate(" + this.c2 + ")");
    }
    
    this.redraw();
}
// Geometry code for http://www.redblobgames.com/articles/curved-paths/
// Copyright 2012 Red Blob Games
// License: Apache v2
var Point = /** @class */ (function () {
    function Point(x, y) {
        this.x = x;
        this.y = y;
    }
    Point.fromPolar = function (r, a) { return new Point(r * Math.cos(a), r * Math.sin(a)); };
    Point.fromObject = function (obj) { return new Point(obj.x, obj.y); };
    Point.prototype.toString = function () { return this.x + "," + this.y; };
    Point.prototype.length_squared = function () { return this.x * this.x + this.y * this.y; };
    Point.prototype.length = function () { return Math.sqrt(this.length_squared()); };
    Point.prototype.normalize = function () { var d = this.length(); return new Point(this.x / d, this.y / d); };
    Point.prototype.scale = function (d) { return new Point(this.x * d, this.y * d); };
    Point.prototype.rotateLeft = function () { return new Point(this.y, -this.x); };
    Point.prototype.rotateRight = function () { return new Point(-this.y, this.x); };
    Point.prototype.add = function (that) { return new Point(this.x + that.x, this.y + that.y); };
    Point.prototype.subtract = function (that) { return new Point(this.x - that.x, this.y - that.y); };
    Point.prototype.dot = function (that) { return this.x * that.x + this.y * that.y; };
    Point.prototype.cross = function (that) { return this.x * that.y - this.y * that.x; };
    Point.prototype.distance = function (that) { return that.subtract(this).length(); };
    return Point;
}());
var Line = /** @class */ (function () {
    function Line(a, b, c) {
        this.a = a;
        this.b = b;
        this.c = c;
    }
    Line.fromRay = function (p, t) {
        var normal = t.rotateLeft().normalize();
        return new Line(normal.x, normal.y, -(normal.x * p.x + normal.y * p.y));
    };
    Line.fromPoints = function (p1, p2) { return Line.fromRay(p1, p2.subtract(p1)); };
    Line.prototype.signedDistanceTo = function (p) {
        return this.a * p.x + this.b * p.y + this.c;
    };
    Line.prototype.pointNearestTo = function (p) {
        var d = this.signedDistanceTo(p);
        return new Point(p.x - d * this.a, p.y - d * this.b);
    };
    return Line;
}());
// Return the intersection of a line p1-p2 and line p3-p4, or null if parallel
function intersectLines(p1, p2, p3, p4) {
    // Intersect two lines, http://paulbourke.net/geometry/pointlineplane/
    var d = 1 / ((p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y));
    var ua = d * ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x));
    var ub = d * ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x));
    if (Math.abs(d) < 1e-9) {
        return null;
    }
    else {
        return p1.add(p2.subtract(p1).scale(ua));
    }
}
// The set of biarc control points that preserve G1 continuity forms a circle.
// Compute center & radius of this circle:
function biarc_valid_control_circle(p1, t1, p2, t2) {
    var v1 = p2.subtract(p1);
    var v2 = p2.add(t2.scale(-100)).subtract(p1.add(t1.scale(100)));
    var m1 = p1.add(v1.scale(0.5));
    var m2 = p1.add(t1.scale(100)).add(v2.scale(0.5));
    var p = intersectLines(m1, m1.add(v1.rotateLeft()), m2, m2.add(v2.rotateLeft()));
    var r = 0;
    if (p == null) {
        p = new Point(-100, -100);
    }
    else {
        r = p1.subtract(p).length();
    }
    return { center: p, radius: r };
}
// Utility code for http://www.redblobgames.com/articles/curved-paths/
// Copyright 2012 Red Blob Games
// License: Apache v2
function clamp(min, max, value) {
    return Math.max(min, Math.min(max, value));
}
// Model/view helpers for http://www.redblobgames.com/articles/curved-paths/
// Copyright 2012 Red Blob Games
// License: Apache v2
// API from https://github.com/amitp/containerport/blob/master/Model.as
var Model = /** @class */ (function () {
    function Model(get, set) {
        this.get = get;
        this.set = set;
    }
    // Factory: connect the model to another object
    Model.ref = function (obj, prop) {
        return new Model(function () { return obj[prop]; }, function (v) { obj[prop] = v; });
    };
    // Factory: connect the model to a constant
    Model.constant = function (value) {
        return new Model(function () { return value; }, function (_) { });
    };
    // Adapter: Number added to constant
    Model.prototype.add = function (n) {
        var _this = this;
        return new Model(function () { return _this.get() + n; }, function (v) { _this.set(v - n); });
    };
    // Adapter: Number multiplied by constant
    Model.prototype.multiply = function (n) {
        var _this = this;
        return new Model(function () { return _this.get() * n; }, function (v) { _this.set(v / n); });
    };
    // Adapter: clamped Number to unclamped Number
    Model.prototype.clamped = function (min, max) {
        var _this = this;
        return new Model(function () { return _this.get(); }, function (v) { _this.set(clamp(min, max, v)); });
    };
    // Adapter: rounded Number to unrounded Number
    Model.prototype.rounded = function (nearest) {
        var _this = this;
        if (nearest === undefined) {
            nearest = 1.0; /* default value */
        }
        return new Model(function () { return _this.get(); }, function (v) { _this.set(nearest * Math.round(v / nearest)); });
    };
    // Adapter: also call a callback after value is set
    Model.prototype.callback = function (fn) {
        var _this = this;
        return new Model(function () { return _this.get(); }, function (v) { _this.set(v); fn(); });
    };
    // Adapter: Point to offset Point. Note that the offset must have .x
    // and .y but it does not have to be a constant. If you modify the
    // offset, the model is not automatically recomputed.
    Model.prototype.offset = function (p) {
        var _this = this;
        return new Model(function () { return _this.get().add(p); }, function (v) { _this.set(v.subtract(p)); });
    };
    // Adapter: distance Number to Point along vector
    Model.prototype.project = function (dir) {
        var _this = this;
        return new Model(function () { return dir.scale(_this.get()); }, function (v) { _this.set(dir.dot(v) / dir.length_squared()); });
    };
    // Binary adapter: Cartesian coordinates
    Model.Cartesian = function (x, y) {
        return new Model(function () { return new Point(x.get(), y.get()); }, function (value) { x.set(value.x); y.set(value.y); });
    };
    // Binary adapter: Polar coordinates
    Model.Polar = function (radius, angle) {
        return new Model(function () {
            var r = radius.get(), a = angle.get();
            return new Point(r * Math.cos(a), r * Math.sin(a));
        }, function (value) {
            radius.set(value.length());
            angle.set(Math.atan2(value.y, value.x));
        });
    };
    return Model;
}());
// Convenience function: map a Point to a Model.Cartesian
function constrained_point(p, xmin, xmax, ymin, ymax) {
    return Model.Cartesian(Model.ref(p, 'x').clamped(xmin, xmax), Model.ref(p, 'y').clamped(ymin, ymax));
}
// Tests
function test_model_js() {
    var data = { x: 0, y: 0, r: 0, a: 0 }; // reference object with underlying data
    var m; // model object being tested
    function EQ(test, expected) {
        if (test != expected) {
            console.log("FAIL", test, "!=", expected);
        }
        else {
            // console.log("SUCC", test, "==", expected);
        }
    }
    function EQ_APPROX(test, expected) {
        return EQ(Math.round(test * 1e6) / 1e6, Math.round(expected * 1e6) / 1e6);
    }
    // Test Number related models
    data.x = 10;
    m = Model.ref(data, 'x');
    EQ(m.get(), 10);
    m.set(1);
    EQ(m.get(), 1);
    EQ(data.x, 1);
    m = Model.constant(7);
    EQ(m.get(), 7);
    m.set(10);
    EQ(m.get(), 7);
    m = Model.ref(data, 'x').add(3);
    m.set(10);
    EQ(m.get(), 10);
    EQ(data.x, 7);
    m = Model.ref(data, 'x').multiply(5);
    m.set(10);
    EQ(m.get(), 10);
    EQ(data.x, 2);
    data.x = 30;
    m = Model.ref(data, 'x').clamped(5, 15);
    EQ(data.x, 30); // initially out of bounds data isn't reset
    EQ(m.get(), 30);
    m.set(1);
    EQ(data.x, 5);
    m.set(100);
    EQ(data.x, 15);
    data.x = 7;
    m = Model.ref(data, 'x').rounded(10);
    EQ(data.x, 7); // initially unrounded data isn't reset
    m.set(7);
    EQ(data.x, 10);
    m.set(141);
    EQ(data.x, 140);
    var called = 0;
    function C() { called++; }
    data.x = 7;
    m = Model.ref(data, 'x').callback(C);
    EQ(data.x, 7);
    EQ(m.get(), 7);
    EQ(called, 0); // callback only on setter, so it's 0 here
    m.set(13);
    EQ(data.x, 13);
    EQ(m.get(), 13);
    EQ(called, 1);
    m.set(2);
    EQ(called, 2);
    // Test Point related models
    var p = Model.Cartesian(Model.ref(data, 'x'), Model.ref(data, 'y'));
    data.x = 10;
    data.y = 20;
    m = p;
    EQ(m.get().x, data.x);
    EQ(data.x, 10);
    EQ(m.get().y, data.y);
    EQ(data.y, 20);
    m.set(new Point(30, 40));
    EQ(data.x, 30);
    EQ(data.y, 40);
    data.x = 10;
    data.y = 20;
    m = p.offset(new Point(3, 7));
    EQ(m.get().x, 13);
    EQ(m.get().y, 27);
    m.set(new Point(30, 40));
    EQ(data.x, 27);
    EQ(data.y, 33);
    data.x = 10;
    var dir = new Point(3, 4);
    m = Model.ref(data, 'x').project(dir);
    EQ(m.get().x, 30);
    EQ(m.get().y, 40);
    m.set(new Point(60, 80));
    EQ(data.x, 20);
    m.set(new Point(-4, 3)); // orthogonal to dir so should be 0
    EQ(data.x, 0);
    data.r = 10;
    data.a = Math.PI * 0.5;
    p = Model.Polar(Model.ref(data, 'r'), Model.ref(data, 'a'));
    EQ_APPROX(data.r, 10);
    EQ_APPROX(data.a, Math.PI * 0.5);
    EQ_APPROX(p.get().x, 0);
    EQ_APPROX(p.get().y, 10);
    p.set(new Point(-5, 0));
    EQ_APPROX(data.r, 5);
    EQ_APPROX(data.a, Math.PI);
    EQ_APPROX(p.get().x, -5);
    EQ_APPROX(p.get().y, 0);
}
test_model_js();
/*
  Inheritance discussion:

  It might be better to have NumberModel extend Model, PointModel extend Model,
  so that we can have .add .multiply .clamped .rounded .project on NumberModel
  and .offset on PointModel. We could also add convenience functions .getX
  .getY to PointModel.

  However, what should Model.constant and Model.ref return? Right now
  it's very generic, allowing any type. One option would be to check
  the type and return a NumberModel or PointModel or Model.

  For now I've left it all in Model.
*/
// Road paths code for http://www.redblobgames.com/articles/curved-paths/
// Copyright 2012 Red Blob Games
// License: Apache v2
var LinePath = /** @class */ (function () {
    function LinePath(p1, p2) {
        this.p1 = p1;
        this.p2 = p2;
        this.v = this.p2.subtract(this.p1);
        this.t = this.v.normalize();
        this.n = this.t.rotateLeft();
    }
    LinePath.prototype.toSvgPath = function () { return ['M', this.p1, 'L', this.p2]; };
    LinePath.prototype.offsetBy = function (w) {
        return new LinePath(this.p1.add(this.n.scale(w)), this.p2.add(this.n.scale(w)));
    };
    return LinePath;
}());
var QuadraticBezierPath = /** @class */ (function () {
    function QuadraticBezierPath(p1, c1, p2) {
        this.p1 = p1;
        this.c1 = c1;
        this.p2 = p2;
        this.t1 = this.c1.subtract(this.p1).normalize();
        this.t2 = this.p2.subtract(this.c1).normalize();
        this.n1 = this.t1.rotateLeft();
        this.n2 = this.t2.rotateLeft();
    }
    QuadraticBezierPath.prototype.toSvgPath = function () { return ['M', this.p1, 'Q', this.c1, this.p2]; };
    QuadraticBezierPath.prototype.offsetBy = function (w) {
        var n = this.p2.subtract(this.p1).normalize().rotateLeft();
        return new QuadraticBezierPath(this.p1.add(this.n1.scale(w)), this.c1.add(n.scale(w)), this.p2.add(this.n2.scale(w)));
    };
    return QuadraticBezierPath;
}());
var CubicBezierPath = /** @class */ (function () {
    function CubicBezierPath(p1, c1, c2, p2) {
        this.p1 = p1;
        this.c1 = c1;
        this.c2 = c2;
        this.p2 = p2;
    }
    CubicBezierPath.prototype.toSvgPath = function () { return ['M', this.p1, 'C', this.c1, this.c2, this.p2]; };
    CubicBezierPath.prototype.offsetBy = function (w) {
        return null;
    };
    return CubicBezierPath;
}());
var ArcPath = /** @class */ (function () {
    function ArcPath(p1, t1, p2) {
        this.p1 = p1;
        this.t1 = t1;
        this.p2 = p2;
        this.v = p2.subtract(p1);
        this.n1 = t1.rotateLeft();
        this.midpoint = this.p1.add(this.v.scale(0.5));
        this.curvature = 2 * this.v.dot(this.n1) / this.v.length_squared();
        this.radius = 1 / this.curvature;
        this.origin = this.p1.add(this.n1.scale(this.radius));
        this.sign_t = this.v.dot(this.t1) > 0;
        this.sign_n = this.v.dot(this.n1) > 0;
        // NOTE: this may be numerically unstable when the curvature
        // goes to 0. The radius will be infinity, the origin will be
        // at infinity, and the normalized direction will be
        // unstable.
        this.n2 = this.origin.subtract(this.p2).normalize().scale(this.sign_n ? 1 : -1);
        this.t2 = this.n2.rotateRight();
        this.a1 = this.toAngle(this.p1);
        this.a2 = this.toAngle(this.p2);
    }
    ArcPath.fromControlPoint = function (p1, c1, p2) {
        return new ArcPath(p1, c1.subtract(p1).normalize(), p2);
    };
    ArcPath.prototype.toAngle = function (p) {
        return Math.atan2(p.y - this.origin.y, p.x - this.origin.x);
    };
    ArcPath.prototype.fromAngle = function (angle) {
        return this.origin.add(Point.fromPolar(this.radius, angle));
    };
    ArcPath.prototype.length = function () {
        // NOTE: sign_t means that the angular span exceeds 180 deg.
        var da = Math.abs(this.a2 - this.a1);
        if (this.sign_t == (da > Math.PI)) {
            da = 2 * Math.PI - da;
        }
        return Math.abs(da * this.radius);
    };
    ArcPath.prototype.toSvgPath = function () {
        return ['M', this.p1,
            'A', this.radius, this.radius,
            0,
            this.sign_t ? 0 : 1,
            this.sign_n ? 0 : 1,
            this.p2
        ];
    };
    ArcPath.prototype.offsetBy = function (w) {
        // NOTE: this only handles offsets of small arcs, which are
        // all that I demonstrate right now in the article.
        return new ArcPath(this.p1.add(this.n1.scale(w)), this.t1, this.p2.add(this.n2.scale(w)));
    };
    return ArcPath;
}());
// Line segment drawing for http://www.redblobgames.com/articles/curved-paths/
// Copyright 2012 Red Blob Games
// License: Apache v2

"use strict";

function redraw_line_measures() {
    var width = 50;
    var v = this.p2.subtract(this.p1);
    var t = v.normalize();
    var u = t.rotateRight();
    var length = v.length();

    this.root.selectAll('.path path')
        .attr('d', ['M', this.p1, 'L', this.p2].join(" "));

    this.root.selectAll('.path path').data([width, width-2, width-4, 1])
        .attr('stroke-width', function(d) { return d; });

    // Draw width
    var q = this.p1.subtract(t.scale(20));
    this.root.selectAll('.line-width-L')
        .attr('d', ['M', q,
                    'm', u.scale(-7),
                    'l', u.scale(-width/2 + 9)].join(" "));
    this.root.selectAll('.line-width-R')
        .attr('d', ['M', q,
                    'm', u.scale(7),
                    'l', u.scale(width/2 - 9)].join(" "));
    this.root.selectAll('.line-width-stop-L')
        .attr('d', ['M', this.p1.add(u.scale(-width/2+1)),
                    'l', t.scale(-40)].join(" "));
    this.root.selectAll('.line-width-stop-R')
        .attr('d', ['M', this.p1.add(u.scale(width/2-1)),
                    'l', t.scale(-40)].join(" "));
    this.root.selectAll('.line-width')
        .attr('x', q.x)
        .attr('y', q.y)
        .attr('transform', "rotate(" + (180/Math.PI * Math.atan2(u.y, u.x)) + " " + q.x + " " + q.y + ")")
        .text(width);
    
    // Draw length
    var midpoint = this.p1.add(v.scale(0.5));
    q = midpoint.add(u.scale(width/2 + 20));
    this.root.selectAll('.line-length-L')
        .attr('d', ['M', q,
                    'm', t.scale(-9),
                    'l', t.scale(-length/2 + 11)].join(" "));
    this.root.selectAll('.line-length-R')
        .attr('d', ['M', q,
                    'm', t.scale(9),
                    'l', t.scale(length/2 - 11)].join(" "));
    this.root.selectAll('.line-length-stop-L')
        .attr('d', ['M', this.p1.add(u.scale(width/2)).add(t),
                    'l', u.scale(40)].join(" "));
    this.root.selectAll('.line-length-stop-R')
        .attr('d', ['M', this.p2.add(u.scale(width/2)).subtract(t),
                    'l', u.scale(40)].join(" "));
    this.root.selectAll('.line-length')
        .attr('x', q.x)
        .attr('y', q.y)
        .attr('transform', "rotate(" + (180/Math.PI * Math.atan2(v.y, v.x)) + " " + q.x + " " + q.y + ")")
        .text(Math.round(length));
}


function redraw_line_offsets() {
    var w = this.w;
    var path1 = new LinePath(this.p1, this.p2);
    var path2 = path1.offsetBy(w);
    var midp1 = path1.p1.add(path2.p1.subtract(path1.p1).scale(0.5));
    this._normal.x = path1.n.x;
    this._normal.y = path1.n.y;
    
    this.root.selectAll(".path path")
        .attr('d', path1.toSvgPath().join(" "));
    this.root.selectAll(".offset-path path")
        .attr('d', path2.toSvgPath().join(" "));
    this.root.selectAll(".slider")
        .attr('transform', "translate(" + this._w_view.get() + ")");
    
    // Draw offset
    var q = path1.p1.add(path1.t.scale(30));
    this.root.selectAll(".line-offset")
        .attr('d', ['M', q,
                    'l', path1.n.scale(w*0.9)].join(" "));

    // Draw label for normal vector
    q = q.add(path1.n.scale(w < 0 ? (w - 8) : (w + 7)));
    this.root.selectAll(".normal-label")
        .attr('transform', "translate(" + q + ") rotate(" + (90 + 180/Math.PI * Math.atan2(path1.n.y, path1.n.x)) + ")")
        .text("N * " + Math.round(w));
}


function redraw_line_distances() {
    var path = new LinePath(this.p1, this.p2);
    var start = path.p1.add(path.n.scale(-30));
    this.root.selectAll('.path path')
        .attr('d', path.toSvgPath().join(" "));
    this._v.x = path.v.x;
    this._v.y = path.v.y;
    this._start.x = start.x - path.n.x * 15;
    this._start.y = start.y - path.n.y * 15;

    var length = this.root.select(".path path").node().getTotalLength();
    var d = this.d * length;
    
    this.root.selectAll(".slider")
        .attr('transform', "translate(" + this._d_view.get() + ")");
    
    // Draw distance
    this.root.selectAll('.length')
        .attr('d', ['M', start,
                    'm', path.t.scale(d < 5? 0 : 2),
                    'l', path.t.scale(Math.max(0, d - 5))].join(" "));
    this.root.selectAll('.length-stop-L')
        .attr('d', ['M', path.p1.add(path.n.scale(2)),
                    'l', path.n.scale(-40)].join(" "));
    this.root.selectAll('.length-stop-R')
        .attr('d', ['M', path.p1.add(path.n.scale(2)).add(path.t.scale(d)),
                    'l', path.n.scale(-40)].join(" "));

    // Draw a vehicle on the path
    var q = path.p1.add(path.t.scale(d));
    var angle = 180/Math.PI * Math.atan2(path.t.y, path.t.x);
    this.root.selectAll('.vehicle')
        .attr('transform', "translate(" + q + ") rotate(" + angle + ")");
    
    // Draw label for tangent vector
    q = start.add(path.t.scale(d + 20));
    this.root.selectAll('.tangent-label')
        .attr('transform', "translate(" + q + ") rotate(" + angle + ")") 
        .text("T * " + Math.round(d));
}


var line_measures = new Diagram(d3.select('svg#line-measures'),
                                function() {
                                    this.p1 = new Point(80, 230);
                                    this.p2 = new Point(400, 70);
                                },
                                redraw_line_measures);
line_measures.attachDragHandleToModels("p1 p2");


var line_offsets = new Diagram(d3.select('svg#line-offsets'),
                               function() {
                                   this.p1 = new Point(40, 80);
                                   this.p2 = new Point(410, 55);
                                   this.w = 30;
                                   this._normal = new Point(0, 1);
                                   this._w_view = Model.ref(this, 'w')
                                       .clamped(-50, 50)
                                       .project(this._normal)
                                       .offset(this.p1);
                               },
                               redraw_line_offsets);
line_offsets.makeHandle("slider", 7);
line_offsets.makeDraggable(line_offsets.root.selectAll(".slider"),
                           line_offsets._w_view);
                                 

var line_distances = new Diagram(d3.select('svg#line-distances'),
                                 function() {
                                     this.p1 = new Point(40, 50);
                                     this.p2 = new Point(409, 25);
                                     this.d = 0.2;
                                     this._v = new Point(0, 100);
                                     this._start = new Point(0, 0);
                                     this._d_view = Model.ref(this, 'd')
                                         .clamped(0, 1)
                                         .project(this._v)
                                         .offset(this._start);
                                 },
                                 redraw_line_distances);
line_distances.makeHandle("slider", 7);
line_distances.makeDraggable(line_distances.root.selectAll(".slider"),
                             line_distances._d_view);
"use strict";

function redraw_bezier_measures() {
    var path = new QuadraticBezierPath(this.p1, this.c1, this.p2);
    
    this.root.selectAll('.path path')
        .attr('d', path.toSvgPath().join(" "));

    this.root.select('.control-polygon')
        .attr('d', ['M', this.p1, 'L', this.c1, 'L', this.p2].join(" "));

    var pathLength = this.root.select('.path path').node().getTotalLength();
    this.root.selectAll('.path-length')
        .text("Path length: " + Math.round(pathLength));
}


function redraw_bezier_offsets() {
    var w = this.w;
    var path1 = new QuadraticBezierPath(this.p1, this.c1, this.p2);
    var path2 = path1.offsetBy(w);
    this._normal.x = path1.n1.x;
    this._normal.y = path1.n1.y;

    this.root.selectAll(".path path")
        .attr('d', path1.toSvgPath().join(" "));
    this.root.selectAll(".offset-path path")
        .attr('d', path2.toSvgPath().join(" "));
    this.root.selectAll(".slider")
        .attr('transform', "translate(" + this._w_view.get() + ")");

    // Draw offset vectors
    this.root.selectAll(".offset-L")
        .attr('d', ['M', path1.p1,
                    'l', path1.n1.scale(w*0.9)].join(" "));
    this.root.selectAll(".offset-R")
        .attr('d', ['M', path1.p2,
                    'l', path1.n2.scale(w*0.9)].join(" "));

    // Draw label for normal vector
    var q = path1.p1.add(path1.t1.scale(10)).add(path1.n1.scale(w < 0 ? (w - 13) : (w + 7)));
    this.root.selectAll(".normal-label")
        .attr('transform', "translate(" + q + ") rotate(" + (90 + 180/Math.PI * Math.atan2(path1.n1.y, path1.n1.x)) + ")")
        .text(Math.round(w) + " * N");
}


function redraw_bezier_distances() {
    var path = new QuadraticBezierPath(this.p1, this.c1, this.p2);
    this.root.selectAll('.path path')
        .attr('d', path.toSvgPath().join(" "));

    this.root.selectAll(".slider")
        .attr('transform', "translate(" + this._slider_view.get() + ")");
    
    // Draw a vehicle on the path
    var node = this.root.select(".path path").node();
    var distance = Math.min(0.999, this.d) * node.getTotalLength();
    var q = Point.fromObject(node.getPointAtLength(distance));
    var t = Point.fromObject(node.getPointAtLength(distance+10)).subtract(q).normalize();
    var n = t.rotateLeft();
    var labelAngle = 180/Math.PI * Math.atan2(t.y, t.x);
    this.root.selectAll('.vehicle')
        .attr('transform', "translate(" + q + ") rotate(" + labelAngle + ")");
    
    // Draw distance
    this.root.selectAll('.length')
        .attr('d', path.offsetBy(30).toSvgPath().join(" "));
    this.root.selectAll('.length-stop-L')
        .attr('d', ['M', path.p1.add(path.n1.scale(2)),
                    'l', path.n1.scale(40)].join(" "));
    this.root.selectAll('.length-stop-R')
        .attr('d', ['M', q.add(n.scale(2)),
                    'l', n.scale(40)].join(" "));

    // Draw label for tangent vector
    this.root.selectAll('.tangent-label')
        .attr('transform', "translate(" + q.add(n.scale(50)) + ") rotate(" + labelAngle + ")")
        .text(Math.round(distance));
}

    
var bezier_measures = new Diagram(d3.select('svg#bezier-measures'),
                                  function() {
                                      this.p1 = new Point(400, 220);
                                      this.p2 = new Point(100, 250);
                                      this.c1 = new Point(300, 50);
                                  },
                                  redraw_bezier_measures);
bezier_measures.attachDragHandleToModels("p1 p2 c1");



var bezier_offsets = new Diagram(d3.select("svg#bezier-offsets-diagram"),
                                 function() {
                                     this.p1 = new Point(100, 110);
                                     this.p2 = new Point(350, 90);
                                     this.c1 = new Point(340, 30);
                                     this._normal = new Point(0, 1);
                                     this.w = 30;
                                     this.t1 = this.c1.subtract(this.p1).normalize();
                                     this._w_view = Model.ref(this, 'w')
                                         .clamped(-50, 50)
                                         .project(this._normal)
                                         .offset(this.p1)
                                         .offset(this.t1.scale(-10));
                                 },
                                 redraw_bezier_offsets);
bezier_offsets.makeHandle("slider", 7);
bezier_offsets.makeDraggable(bezier_offsets.root.selectAll(".slider"),
                             bezier_offsets._w_view);


var bezier_distances = new Diagram(d3.select('svg#bezier-distances-diagram'),
                                   function() {
                                       this.p1 = new Point(100, 110);
                                       this.p2 = new Point(350, 90);
                                       this.c1 = new Point(300, 30);
                                       this.d = 0.5;
                                       this._slider_view = Model.ref(this, 'd')
                                           .clamped(0, 1)
                                           .project(new Point(400, 0))
                                           .offset(new Point(25, 140));
                                   },
                                   redraw_bezier_distances);
bezier_distances.makeHandle("slider", 7);
bezier_distances.makeDraggable(bezier_distances.root.selectAll(".slider"),
                               bezier_distances._slider_view);
"use strict";

function redraw_arc_measures() {
    // Update the midpoint point object that's used for the control point
    this.midpoint.x = 0.5 * (this.p1.x + this.p2.x);
    this.midpoint.y = 0.5 * (this.p1.y + this.p2.y);

    var n = this.p2.subtract(this.p1).normalize().rotateLeft();
    this._control_dir.x = n.x;
    this._control_dir.y = n.y;
    var control = this._r_view.get();

    var path = new ArcPath(this.p1,
                       control.subtract(this.p1).normalize(),
                       this.p2);

    this.root.selectAll('.control')
        .attr('transform', "translate(" + control + ")");

    this.root.selectAll('.n1-arrow')
        .attr('d', ['M', path.p1, 'l', path.n1.scale(40)].join(" "));
    this.root.selectAll('.n2-arrow')
        .attr('d', ['M', path.p2, 'l', path.n2.scale(40)].join(" "));
    this.root.select('.control-polygon')
        .attr('d', ['M', this.p1, 'L', control, 'L', this.p2].join(" "));

    this.root.selectAll('.path path')
        .attr('d', path.toSvgPath().join(" "));

    this.root.selectAll('.path-length')
        .text("Path length: " + Math.round(this.root.select('.path path').node().getTotalLength())
              + " curvature: " + (1000*path.curvature).toFixed(2) + "/1000"
              + " radius: " + Math.round(Math.abs(path.radius))
              );

    this.root.selectAll('.full-circle')
        .attr('transform', "translate(" + path.p1.add(path.n1.scale(path.radius)) + ")")
        .attr('r', Math.abs(path.radius) > 1e3 ? 0 : Math.abs(path.radius));
}


function redraw_arc_offsets() {
    var w = this.w;
    var path1 = new ArcPath(this.p1, this.t1, this.p2);
    var path2 = path1.offsetBy(w);
    this._normal.x = path1.n1.x;
    this._normal.y = path1.n1.y;

    this.root.selectAll(".path path")
        .attr('d', path1.toSvgPath().join(" "));
    this.root.selectAll(".offset-path path")
        .attr('d', path2.toSvgPath().join(" "));
    this.root.selectAll(".slider")
        .attr('transform', "translate(" + this._w_view.get() + ")");

    // Draw offset vectors
    this.root.selectAll(".offset-L")
        .attr('d', ['M', path1.p1,
                    'l', path1.n1.scale(w*0.9)].join(" "));
    this.root.selectAll(".offset-R")
        .attr('d', ['M', path1.p2,
                    'l', path1.n2.scale(w*0.9)].join(" "));

    // Draw label for normal vector
    var q = path1.p1.add(path1.t1.scale(10)).add(path1.n1.scale(w < 0 ? (w - 13) : (w + 7)));
    this.root.selectAll(".normal-label")
        .attr('transform', "translate(" + q + ") rotate(" + (90 + 180/Math.PI * Math.atan2(path1.n1.y, path1.n1.x)) + ")")
        .text(Math.round(w) + " * N");
}


function redraw_arc_distances() {
    var path = this._path;
    this.root.selectAll('.path path')
        .attr('d', path.toSvgPath().join(" "));

    this.root.selectAll(".slider")
        .attr('transform', "translate(" + this._slider_view.get() + ")");

    // Draw a vehicle on the path
    var q = path.fromAngle(Math.PI + this.angle);
    var n = Point.fromPolar(1, this.angle);
    var t = n.rotateRight();
    var labelAngle = 180/Math.PI * Math.atan2(t.y, t.x);
    this.root.selectAll('.vehicle')
        .attr('transform', "translate(" + q + ") rotate(" + labelAngle + ")");

    // Draw distance
    this.root.selectAll('.length')
        .attr('d', path.offsetBy(30).toSvgPath().join(" "));
    this.root.selectAll('.length-stop-L')
        .attr('d', ['M', path.p1.add(path.n1.scale(2)),
                    'l', path.n1.scale(40)].join(" "));
    this.root.selectAll('.length-stop-R')
        .attr('d', ['M', q.add(n.scale(2)),
                    'l', n.scale(40)].join(" "));

    // Draw label for tangent vector
    this.root.selectAll('.tangent-label')
        .attr('transform', "translate(" + q.add(n.scale(50)) + ") rotate(" + labelAngle + ")")
        .text(Math.round(180/Math.PI * this.angle) + " deg");
}


var arc_measures = new Diagram(d3.select("svg#arc-measures"),
                               function() {
                                   this.p1 = new Point(100, 250);
                                   this.p2 = new Point(350, 220);
                                   this.r = 150;
                                   this._control_dir = new Point(0, 1);
                                   this.midpoint = this.p1.add(this.p2.subtract(this.p1).scale(0.5));

                                   this._r_view = Model.ref(this, 'r')
                                       .clamped(-500, 500)
                                       .project(this._control_dir)
                                       .offset(this.midpoint);
                               },
                               redraw_arc_measures);
arc_measures.attachDragHandleToModels("p1 p2");
arc_measures.makeHandle('control', 9);
arc_measures.makeDraggable(arc_measures.root.selectAll(".control"), arc_measures._r_view);


var arc_offsets = new Diagram(d3.select("svg#arc-offsets"),
                               function() {
                                   this.p1 = new Point(100, 130);
                                   this.p2 = new Point(350, 110);
                                   this.t1 = new Point(2, -3).normalize();
                                   this._normal = new Point(0, 1);
                                   this.w = 30;
                                   this._w_view = Model.ref(this, 'w')
                                       .clamped(-50, 50)
                                       .project(this._normal)
                                       .offset(this.p1)
                                       .offset(this.t1.scale(-10));
                               },
                               redraw_arc_offsets);
arc_offsets.makeHandle("slider", 7);
arc_offsets.makeDraggable(arc_offsets.root.selectAll(".slider"), arc_offsets._w_view);


var arc_distances = new Diagram(d3.select('svg#arc-distances'),
                                function() {
                                    var path = new ArcPath(new Point(100, 130),
                                                           new Point(2, -3).normalize(),
                                                           new Point(350, 110));
                                    this.angle = -0.5 * Math.PI;
                                    this._path = path;
                                    this._angle_view = Model.ref(this, 'angle')
                                        .clamped(path.toAngle(path.p1),
                                                 path.toAngle(path.p2));
                                    this._slider_view = Model.Polar(Model.constant(Math.abs(path.radius) - 30),
                                                                    this._angle_view)
                                        .offset(path.origin);
                                },
                                redraw_arc_distances);
arc_distances.makeHandle("slider", 7);
arc_distances.makeDraggable(arc_distances.root.selectAll(".slider"), arc_distances._slider_view);


function redraw_biarc_angles() {
    // The biarc consists of two arcs, meeting at a joint point:
    var path1 = new ArcPath(this.p1, Point.fromPolar(1, this.a1), this.c1);
    var path2 = new ArcPath(this.p2, Point.fromPolar(1, this.a2), this.c1);
    this.root.selectAll('.path path')
        .attr('d', (path1.toSvgPath().concat(path2.toSvgPath())).join(" "));

    // Control points that preserve the tangent all lie on a circle:
    var control = biarc_valid_control_circle(this.p1, path1.t1, this.p2, path2.t1);

    // Draw the valid joint point circle with color coding to show how
    // good each spot is.  TODO: choose points non-uniformly; we want
    // more points between the two biarc endpoints, and/or near the
    // optimum joint point. TODO: might be useful to incorporate
    // curvature into the score, not only road length.
    var angles = new Array(43);
    var dAngle = Math.PI * 2 / angles.length;
    for (var i = 0; i < angles.length; i++) {
        angles[i] = dAngle * i;
    }

    this.root.select(".valid-joint-eval").selectAll("path")
        .data(angles)
      .enter()
        .append('path')
        .attr('stroke-width', 12)
        .attr('stroke-linecap', "butt")
        .attr('stroke-opacity', 0.5)
        .attr('fill', "none");

    var that = this;
    this.root.select(".valid-joint-eval").selectAll("path")
        .attr('d', function(angle) {
            if (isFinite(control.radius) && isFinite(control.center.x)) {
                var left = control.center.add(Point.fromPolar(control.radius, angle - dAngle/2));
                var right = control.center.add(Point.fromPolar(control.radius, angle + dAngle/2));
                return ['M', left, 'A', control.radius, control.radius, 0, 0, 1, right].join(" ");
            } else {
                // NOTE: to do this right we'd want to calculate
                // things differently to avoid the infinities, but I
                // don't want to spend that time on this demo
                return "M 0 0";
            }
        })
        .attr('stroke', function(angle) {
            var joint = control.center.add(Point.fromPolar(control.radius, angle));
            var path1 = new ArcPath(that.p1, Point.fromPolar(1, that.a1), joint);
            var path2 = new ArcPath(that.p2, Point.fromPolar(1, that.a2), joint);
            var magic = 0.4;
            var score = magic * (path1.length() + path2.length()) / control.radius;
            // NOTE: IE9 needs .toString() but in Firefox and Chrome it doesn't.
            return d3.hsl(170 / Math.max(1.0, score), 0.5, 0.5).toString();
        });
}


var arc_biarc_angles = new Diagram(d3.select("svg#arc-biarc-angles"),
                                   function() {
                                       this.p1 = new Point(100.3, 133.3);
                                       this.p2 = new Point(353.3, 166.6);
                                       this.c1 = new Point(200.4, 200.1);
                                       this.a1 = 0.25 * Math.PI;
                                       this.a2 = 0.25 * Math.PI;
                                   },
                                   redraw_biarc_angles);
arc_biarc_angles.attachDragHandleToModels("p1 p2 c1 t1 t2");
arc_biarc_angles.root.select(".c1.draggable").attr('class', "c1 draggable green");



function redraw_biarc_chain() {
    // Choose control points automatically
    var minRadius = 20;
    for (var i = 0; i < this.N; i++) {
        var control = biarc_valid_control_circle(
            this.points[i].p, Point.fromPolar(1, this.points[i].a),
            this.points[i+1].p, Point.fromPolar(-1, this.points[i+1].a));
        var bestScore = Infinity, numAngles = 37;
        for (var j = 0; j < numAngles; j++) {
            var angle = 2 * Math.PI * j / numAngles;
            var joint = control.center.add(Point.fromPolar(control.radius, angle));
            var path1 = new ArcPath(this.points[i].p, Point.fromPolar(1, this.points[i].a), joint);
            var path2 = new ArcPath(this.points[i+1].p, Point.fromPolar(-1, this.points[i+1].a), joint);
            var score = path1.length() + path2.length();
            if (isFinite(score) && score < bestScore
                && (!isFinite(bestScore) || (Math.abs(path1.radius) > minRadius
                                             && Math.abs(path2.radius) > minRadius))) {
                bestScore = score;
                this.points[i].c = joint;
            }
        }
    }

    // Move control points to lie on control circle
    /*
    for (var i = 0; i < this.N; i++) {
        var control = biarc_valid_control_circle(
            this.points[i].p, Point.fromPolar(1, this.points[i].a),
            this.points[i+1].p, Point.fromPolar(-1, this.points[i+1].a));
        var delta = this.points[i].c.subtract(control.center);
        var angle = Math.atan2(delta.y, delta.x);
        this.points[i].c = control.center.add(Point.fromPolar(control.radius, angle));
    }
    */

    // Update the UI drag handles
    this.root.selectAll(".overlay .p").data(this.points)
        .attr('transform', function(d) { return "translate(" + d.p + ")"; });
    this.root.selectAll(".underlay .p").data(this.points)
        .attr('transform', function(d) { return "translate(" + d.p + ")"; });
    this.root.selectAll(".overlay .c").data(this.points)
        .attr('transform', function(d) { return "translate(" + d.c + ")"; });
    this.root.selectAll(".underlay .c").data(this.points)
        .attr('transform', function(d) { return "translate(" + d.c + ")"; });
    this.root.selectAll(".overlay .t").data(this.points)
        .attr('transform', function(d) { return "translate(" + d.view.get() + ") rotate(" + (d.a * 180 / Math.PI) + ")"; });
    this.root.selectAll(".underlay .t").data(this.points)
        .attr('transform', function(d) { return "translate(" + d.view.get() + ")"; });

    // Draw the biarcs
    var that = this;
    var path = [];
    for (var i = 0; i < this.N; i++) {
        var path1 = new ArcPath(that.points[i].p,
                                Point.fromPolar(1, that.points[i].a),
                                that.points[i].c);
        var path2 = new ArcPath(that.points[i+1].p,
                                Point.fromPolar(-1, that.points[i+1].a),
                                that.points[i].c);

        // Cut up this path and make it something that can join with other paths
        var svg = path1.toSvgPath().concat(path2.toSvgPath().slice(2));
        svg[svg.length-2] = 1 - svg[svg.length-2];
        svg[svg.length-1] = that.points[i+1].p;
        if (path.length > 0) {
            svg = svg.slice(2);
        }
        path = path.concat(svg);
    }
    this.root.selectAll(".pathcontainer path")
        .attr('d', path.join(" "));
}

var biarc_chain = new Diagram(d3.select("svg#biarc-chain"),
                              function() {
                                  this.N = 5;
                                  this.points = [];

                                  for (var i = 0; i <= this.N; i++) {
                                      this.points.push({p: new Point(225, 225).add(Point.fromPolar(150, 1.2*Math.PI*i/this.N)),
                                                        a: (0.25 + 1.2 * i) * Math.PI,
                                                        c: new Point(75 + 150*i, (i % 2 == 0)? 50 : 150)});
                                      this.points[i].view = Model.Polar(Model.constant(40),
                                                                        Model.ref(this.points[i], 'a'))
                                          .offset(this.points[i].p);

                                      this.makeHandle("p" + i, 11);
                                      this.makeHandle("t" + i, 7);
                                      this.makeDraggable(this.root.selectAll(".p" + i),
                                                         this.constrainPoint(this.points[i].p));
                                      this.makeDraggable(this.root.selectAll(".t" + i),
                                                         this.points[i].view);
                                      if (i < this.N) {
                                          this.makeHandle("c" + i, 4);
                                          /*
                                          this.makeDraggable(this.root.selectAll(".c" + i),
                                                             this.constrainPoint(this.points[i].c));
                                          */
                                      }
                                  }

                                  this.root.selectAll(".c").classed('green', true);

                                  var path = this.root.select(".pathcontainer");

                                  path.append('path')
                                      .attr('class', "pavement")
                                      .attr('d', "M 0 0")
                                      .attr('stroke-width', 21);
                                  path.append('path')
                                      .attr('class', "pavement-white-stripe")
                                      .attr('d', "M 0 0")
                                      .attr('stroke-width', 19);
                                  path.append('path')
                                      .attr('class', "pavement")
                                      .attr('d', "M 0 0")
                                      .attr('stroke-width', 17);
                                  path.append('path')
                                      .attr('class', "pavement-yellow-stripe")
                                      .attr('d', "M 0 0");
                              },
                              redraw_biarc_chain);
function redraw_comparison() {
    this.midpoint.x = 0.5 * (this.p1.x + this.p2.x);
    this.midpoint.y = 0.5 * (this.p1.y + this.p2.y);

    var v = this.p2.subtract(this.p1);
    var t = v.normalize();
    var n = t.rotateLeft();
    this._control_dir.x = n.x;
    this._control_dir.y = n.y;
    var control = this._r_view.get();

    this.root.selectAll(".control")
        .attr('transform', "translate(" + control + ")");

    // Construct a bezier curve that passes through this point instead
    // of treating it as a regular control point
    var bezier_control = control.subtract(this.midpoint).scale(2).add(this.midpoint);

    var bezier_path = new QuadraticBezierPath(this.p1,
                                     bezier_control,
                                     this.p2);
    
    // Construct an arc that passes through the control point
    var m1 = this.p1.add(control.subtract(this.p1).scale(0.5));
    var m2 = this.p2.add(control.subtract(this.p2).scale(0.5));
    var t1 = control.subtract(this.p1);
    var t2 = control.subtract(this.p2);
    var p = intersectLines(m1, m1.add(t1.rotateLeft()), m2, m2.add(t2.rotateLeft()));
    var np = this.p1.subtract(p).rotateLeft().normalize();
    var arc_path = new ArcPath(this.p1,
                               // probably exists a cleaner way to do this
                               // but this is just for a demo:
                               np.scale(np.dot(t1) > 0? 1 : -1),
                               this.p2);

    this.root.selectAll(".path-bezier path")
        .attr('d', bezier_path.toSvgPath().join(" "));
    
    this.root.selectAll(".path-arc path")
        .attr('d', arc_path.toSvgPath().join(" "));

    var d = bezier_control.subtract(this.midpoint).normalize().scale(400);
    this.root.selectAll(".clip-arc")
        .attr('d', ['M', this.midpoint,
                    'l', d,
                    'l', t.scale(400),
                    'l', d.scale(-2),
                    'l', t.scale(-400),
                    'l', d,
                    ].join(" "));

    this.root.selectAll(".clip-bezier")
        .attr('d', ['M', this.midpoint,
                    'l', d,
                    'l', t.scale(-400),
                    'l', d.scale(-2),
                    'l', t.scale(400),
                    'l', d,
                    ].join(" "));

    this.root.selectAll(".text-bezier")
        .attr('transform', "translate(" + this.midpoint.add(t.scale(-75)) + ")");
    this.root.selectAll(".text-arc")
        .attr('transform', "translate(" + this.midpoint.add(t.scale(75)) + ")");
}


var comparison = new Diagram(d3.select("svg#comparison"),
                               function() {
                                   this.p1 = new Point(35, 240);
                                   this.p2 = new Point(415, 240);
                                   this.r = 175;
                                   this._control_dir = new Point(0, 1);
                                   this.midpoint = this.p1.add(this.p2.subtract(this.p1).scale(0.5));
                                   
                                   this._r_view = Model.ref(this, 'r')
                                       .clamped(-500, 500)
                                       .project(this._control_dir)
                                       .offset(this.midpoint);
                               },
                               redraw_comparison);
comparison.attachDragHandleToModels("p1 p2");
comparison.makeHandle('control', 9);
comparison.makeDraggable(comparison.root.selectAll(".control"), comparison._r_view);

#endif