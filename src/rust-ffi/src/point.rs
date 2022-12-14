use curve25519_dalek_ng::{
    ristretto::{CompressedRistretto, RistrettoPoint},
    scalar::Scalar,
};
use std::{ptr, slice};

#[no_mangle]
pub unsafe extern "C" fn ristretto_point_from_uniform_bytes(
    bytes: *const u8,
) -> *const RistrettoPoint {
    let bytes = slice::from_raw_parts(bytes, 64).try_into().unwrap();
    Box::into_raw(Box::new(RistrettoPoint::from_uniform_bytes(bytes)))
}

#[no_mangle]
pub unsafe extern "C" fn ristretto_point_gut_spill(this: *const RistrettoPoint) {
    let this = &*this;
    println!("My Guts: {:?}", this.compress());
}

#[no_mangle]
pub unsafe extern "C" fn ristretto_point_compress(this: *const RistrettoPoint, dst: *mut u8) {
    let this = &*this;
    let dst = slice::from_raw_parts_mut(dst, 32);
    let src = this.compress().to_bytes();
    dst.clone_from_slice(&src);
}

#[no_mangle]
pub extern "C" fn ristretto_point_decompress(bytes: *const u8) -> *const RistrettoPoint {
    let bytes = unsafe { slice::from_raw_parts(bytes, 32) };
    let compressed = CompressedRistretto::from_slice(bytes);
    let Some(point) = compressed.decompress() else {
        return ptr::null(); // follow C-style error convetion
    };
    Box::into_raw(Box::new(point))
}

#[no_mangle]
pub unsafe extern "C" fn ristretto_point_equals(
    lhs: *const RistrettoPoint,
    rhs: *const RistrettoPoint,
) -> bool {
    let lhs = &*lhs;
    let rhs = &*rhs;
    lhs == rhs
}

#[no_mangle]
pub unsafe extern "C" fn ristretto_point_add(
    lhs: *const RistrettoPoint,
    rhs: *const RistrettoPoint,
) -> *const RistrettoPoint {
    let lhs = &*lhs;
    let rhs = &*rhs;
    Box::into_raw(Box::new(lhs + rhs))
}


#[no_mangle]
pub unsafe extern "C" fn ristretto_point_sub(
    lhs: *const RistrettoPoint,
    rhs: *const RistrettoPoint,
) -> *const RistrettoPoint {
    let lhs = &*lhs;
    let rhs = &*rhs;
    Box::into_raw(Box::new(lhs - rhs))
}


#[no_mangle]
pub unsafe extern "C" fn ristretto_point_negate(
    this: *const RistrettoPoint,
) -> *const RistrettoPoint {
    let this = &*this;
    Box::into_raw(Box::new(-this))
}

#[no_mangle]
pub unsafe extern "C" fn ristretto_point_mul_bytes(
    lhs: *const RistrettoPoint,
    rhs: *const u8,
) -> *const RistrettoPoint {
    let lhs = &*lhs;

    let rhs = slice::from_raw_parts(rhs, 32);
    let rhs = Scalar::from_bytes_mod_order(rhs.try_into().unwrap());
    Box::into_raw(Box::new(lhs * rhs))
}

#[no_mangle]
pub unsafe extern "C" fn ristretto_point_mul_scalar(
    lhs: *const RistrettoPoint,
    rhs: *const Scalar,
) -> *const RistrettoPoint {
    let lhs = &*lhs;
    let rhs = &*rhs;
    Box::into_raw(Box::new(lhs * rhs))
}

#[no_mangle]
pub extern "C" fn ristretto_point_free(this: *mut RistrettoPoint) {
    if this.is_null() {
        return;
    }
    unsafe {
        drop(Box::from_raw(this));
    }
}
